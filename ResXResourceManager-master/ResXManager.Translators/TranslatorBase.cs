namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    [DataContract]
    public abstract class TranslatorBase : ObservableObject, ITranslator
    {
        [NotNull]
        private static readonly Regex _removeKeyboardShortcutIndicatorsRegex = new Regex(@"[&_](?=[\w\d])", RegexOptions.Compiled);

        [CanBeNull]
        protected static readonly IWebProxy WebProxy;

        static TranslatorBase()
        {
            try
            {
                WebProxy = WebRequest.DefaultWebProxy ?? new WebProxy();
                WebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            catch
            {
                // ignored
            }
        }

        protected TranslatorBase([NotNull] string id, [NotNull] string displayName, [CanBeNull] Uri uri, [CanBeNull][ItemNotNull] IList<ICredentialItem> credentials)
        {
            Contract.Requires(id != null);
            Contract.Requires(displayName != null);

            Id = id;
            DisplayName = displayName;
            Uri = uri;
            Credentials = credentials ?? new ICredentialItem[0];
        }

        public string Id { get; }

        public string DisplayName { get; }

        public Uri Uri { get; }

        [DataMember]
        public bool IsEnabled { get; set; } = true;

        [DataMember]
        public bool SaveCredentials { get; set; }

        public IList<ICredentialItem> Credentials { get; }

        public abstract void Translate(ITranslationSession translationSession);

        [NotNull]
        protected static string RemoveKeyboardShortcutIndicators([NotNull] string value)
        {
            return _removeKeyboardShortcutIndicatorsRegex.Replace(value, string.Empty);
        }
    }
}