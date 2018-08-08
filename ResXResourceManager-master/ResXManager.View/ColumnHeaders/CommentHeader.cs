﻿namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    public class CommentHeader : LanguageColumnHeaderBase
    {
        public CommentHeader([NotNull] Configuration configuration, [NotNull] CultureKey cultureKey)
            : base(configuration, cultureKey)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(cultureKey != null);
        }

        public override ColumnType ColumnType => ColumnType.Comment;

        public override string ToString()
        {
            return Resources.Comment;
        }
    }
}
