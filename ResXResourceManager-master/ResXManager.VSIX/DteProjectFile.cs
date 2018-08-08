﻿namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    internal class DteProjectFile : ProjectFile
    {
        [NotNull]
        private readonly DteSolution _solution;
        [NotNull]
        [ItemNotNull]
        private readonly List<EnvDTE.ProjectItem> _projectItems = new List<EnvDTE.ProjectItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DteProjectFile" /> class.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="filePath">Name of the file.</param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="uniqueProjectName">Unique name of the project file.</param>
        /// <param name="projectItem">The project item, or null if the projectItem is not known.</param>
        public DteProjectFile([NotNull] DteSolution solution, [NotNull] string filePath, [CanBeNull] string projectName, [CanBeNull] string uniqueProjectName, [NotNull] EnvDTE.ProjectItem projectItem)
            : base(filePath, solution.SolutionFolder, projectName, uniqueProjectName)
        {
            Contract.Requires(solution != null);
            Contract.Requires(!string.IsNullOrEmpty(filePath));
            Contract.Requires(projectItem != null);

            _solution = solution;
            _projectItems.Add(projectItem);
        }

        /// <summary>
        /// Gets the project items.
        /// </summary>
        [NotNull, ItemNotNull]
        public IList<EnvDTE.ProjectItem> ProjectItems
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<EnvDTE.ProjectItem>>() != null);
                Contract.Ensures(Contract.Result<IList<EnvDTE.ProjectItem>>().Count > 0);

                return _projectItems;
            }
        }

        public void AddProject([NotNull] string projectName, [NotNull] EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectName != null);
            Contract.Requires(projectItem != null);

            _projectItems.Add(projectItem);
            ProjectName += @", " + projectName;
        }

        protected override XDocument InternalLoad()
        {
            var projectItem = DefaultProjectItem;

            try
            {
                return projectItem.TryGetContent() ?? base.InternalLoad();
            }
            catch (IOException)
            {
                // The file does not exist locally, but VS may download it when we call projectItem.Open()
            }

            projectItem.Open();
            return projectItem.TryGetContent() ?? new XDocument();
        }

        protected override void InternalChanged(XDocument document, bool willSaveImmediately)
        {
            var projectItem = DefaultProjectItem;

            try
            {
                if (!willSaveImmediately)
                    projectItem.Open();

                if (projectItem.TrySetContent(document))
                {
                    HasChanges = !projectItem.Document?.Saved ?? false;
                    return;
                }
            }
            catch
            {
                // in case of errors write directly to the file...
            }

            base.InternalChanged(document, willSaveImmediately);
        }

        protected override void InternalSave(XDocument document)
        {
            var projectItem = DefaultProjectItem;

            try
            {
                if (projectItem.Document?.Save() == EnvDTE.vsSaveStatus.vsSaveSucceeded)
                    return;
            }
            catch
            {
                // in case of errors write directly to the file...
            }

            base.InternalSave(document);
        }

        public override bool IsWritable => !DefaultProjectItem.TryGetDocument()?.ReadOnly ?? base.IsWritable;

        public CodeGenerator CodeGenerator
        {
            get => GetCodeGenerator();
            set
            {
                if (GetCodeGenerator() != value)
                    SetCodeGenerator(value);

                OnPropertyChanged();
            }
        }

        [NotNull]
        public EnvDTE.ProjectItem DefaultProjectItem
        {
            get
            {
                Contract.Ensures(Contract.Result<EnvDTE.ProjectItem>() != null);

                var item = ProjectItems.First();
                Contract.Assume(item != null);
                return item;
            }
        }

        [CanBeNull]
        public EnvDTE.ProjectItem ParentItem
        {
            get
            {
                try
                {
                    return DefaultProjectItem.Collection?.Parent as EnvDTE.ProjectItem;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public override bool IsWinFormsDesignerResource
        {
            get
            {
                try
                {
                    var projectItem = DefaultProjectItem;
                    var projectItems = projectItem.Collection;

                    var parent = projectItems?.Parent as EnvDTE.ProjectItem;
                    var subType = parent?.GetProperty(@"SubType") as string;

                    return (subType == @"Form") || (subType == @"UserControl");
                }
                catch (ExternalException)
                {
                }

                return false;
            }
        }

        private CodeGenerator GetCodeGenerator()
        {
            try
            {
                var projectItem = DefaultProjectItem;
                var containingProject = projectItem.ContainingProject;

                if ((containingProject == null) || (containingProject.Kind != ItemKind.CSharpProject))
                    return CodeGenerator.None;

                var customTool = projectItem.GetCustomTool();

                if (string.IsNullOrEmpty(customTool))
                {
                    if (IsWinFormsDesignerResource)
                        return CodeGenerator.WinForms;

                    return projectItem.Children().Any(IsTextTemplate) ? CodeGenerator.TextTemplate : CodeGenerator.None;
                }

                return Enum.TryParse(customTool, out CodeGenerator codeGenerator) ? codeGenerator : CodeGenerator.Unknown;
            }
            catch (ExternalException)
            {
            }

            return CodeGenerator.Unknown;
        }

        private void SetCodeGenerator(CodeGenerator value)
        {
            try
            {
                foreach (var projectItem in ProjectItems)
                {
                    Contract.Assume(projectItem != null);
                    var containingProject = projectItem.ContainingProject;

                    if ((containingProject == null) || (containingProject.Kind != ItemKind.CSharpProject))
                        return;

                    switch (value)
                    {
                        case CodeGenerator.ResXFileCodeGenerator:
                        case CodeGenerator.PublicResXFileCodeGenerator:
                            SetCustomToolCodeGenerator(projectItem, value);
                            break;

                        case CodeGenerator.TextTemplate:
                            SetTextTemplateCodeGenerator(projectItem);
                            break;
                    }
                }
            }
            catch (ExternalException)
            {
            }
        }

        private static bool IsTextTemplate([NotNull] EnvDTE.ProjectItem item)
        {
            Contract.Requires(item != null);

            var name = item.Name;

            return (name != null) && name.EndsWith(@".tt", StringComparison.OrdinalIgnoreCase);
        }

        private void SetTextTemplateCodeGenerator([NotNull] EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(projectItem != null);

            projectItem.SetCustomTool(null);

            const string t4FileName = "Resources.Designer.t4";

            if (!_solution.GetProjectFiles().Any(file => file.RelativeFilePath.Equals(t4FileName)))
            {
                var fullName = Path.Combine(_solution.SolutionFolder, t4FileName);
                File.WriteAllBytes(fullName, Resources.Resources_Designer_t4);
                _solution.AddFile(fullName);
            }

            // Ensure DataAnnotations is referenced, used by TT generated code.
            const string dataAnnotations = "System.ComponentModel.DataAnnotations";

            var vsProject = projectItem.ContainingProject?.Object as VSLangProj.VSProject;
            vsProject?.References?.Add(dataAnnotations);

            var fileName = Path.ChangeExtension(FilePath, "Designer.tt");

            File.WriteAllBytes(fileName, Resources.Resources_Designer_tt);

            var item = projectItem.AddFromFile(fileName);
            if (item == null)
                return;

            item.SetProperty(@"BuildAction", 0);

            Dispatcher.BeginInvoke(() => item.RunCustomTool());
        }

        private static void SetCustomToolCodeGenerator([NotNull] EnvDTE.ProjectItem projectItem, CodeGenerator value)
        {
            Contract.Requires(projectItem != null);

            projectItem.Children()
                .Where(IsTextTemplate)
                .ToArray()
                .ForEach(i => i.Delete());

            projectItem.SetCustomTool(value.ToString());
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_solution != null);
            Contract.Invariant(_projectItems != null);
            Contract.Invariant(_projectItems.Count > 0);
        }
    }
}