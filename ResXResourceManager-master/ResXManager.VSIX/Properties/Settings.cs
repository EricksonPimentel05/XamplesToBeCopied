﻿namespace tomenglertde.ResXManager.VSIX.Properties
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    using TomsToolbox.Core;
    using TomsToolbox.ObservableCollections;

    public sealed partial class Settings
    {
        [NotNull]
        private readonly ObservableIndexer<string, int> _moveToResourcePreferedReplacementPatternIndex = new ObservableIndexer<string, int>(_ => 0);

        static Settings()
        {
            Default.PropertyChanged += (sender, _) => ((Settings)sender).Save();
        }

        public Settings()
        {
            try
            {
                var values = JsonConvert.DeserializeObject<KeyValuePair<string, int>[]>(MoveToResourcePreferedReplacementPatterns);
                values?.ForEach(value => _moveToResourcePreferedReplacementPatternIndex[value.Key] = value.Value);
            }
            catch
            {
                // invalid source, go with default...
            }

            _moveToResourcePreferedReplacementPatternIndex.CollectionChanged += (_, __) => MoveToResource_PreferedReplacementPatternIndex_Changed();
            _moveToResourcePreferedReplacementPatternIndex.PropertyChanged += (_, __) => MoveToResource_PreferedReplacementPatternIndex_Changed();
        }

        [NotNull]
        public ObservableIndexer<string, int> MoveToResourcePreferedReplacementPatternIndex
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableIndexer<string, int>>() != null);
                return _moveToResourcePreferedReplacementPatternIndex;
            }
        }

        private void MoveToResource_PreferedReplacementPatternIndex_Changed()
        {
            MoveToResourcePreferedReplacementPatterns = JsonConvert.SerializeObject(_moveToResourcePreferedReplacementPatternIndex);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_moveToResourcePreferedReplacementPatternIndex != null);
        }
    }
}
