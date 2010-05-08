using System;
using System.Collections.Generic;

namespace QRBuild.Engine
{
    /// 
    public sealed class BuildGraph
    {
        //-- Internal interface

        /// Add a translation to the BuildGraph.
        /// BuildTranslation's constructor calls this, so that users don't have to.
        /// A BuildTranslation's inputs and outputs must not change after a BuildGraph.Execute() call, since
        /// that will cause dependency-analysis to be out-of-sync with the BuildTranslation.
        internal void Add(BuildTranslation translation)
        {
            m_translations.Add(translation);
            m_dependenciesComputed = false;
        }


        //-- Members

        private bool m_dependenciesComputed;
        private bool m_dependenciesValid;

        private readonly HashSet<BuildTranslation> m_translations = new HashSet<BuildTranslation>();
    }
}
