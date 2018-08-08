﻿namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    [Export(typeof(IConfiguration))]
    [Export(typeof(Configuration))]
    public class StandaloneConfiguration : Configuration
    {
        [ImportingConstructor]
        public StandaloneConfiguration([NotNull] ITracer tracer)
            : base(tracer)
        {
            Contract.Requires(tracer != null);
        }

        public override bool IsScopeSupported => false;

        public override ConfigurationScope Scope => ConfigurationScope.Global;
    }
}
