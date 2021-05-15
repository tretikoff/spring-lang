using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Spring
{
    [ProjectFileTypeDefinition(Name)]
    public class SpringProjectFileType : KnownProjectFileType
    {
        public const string Name = "Spring";
        [CanBeNull]
        [UsedImplicitly]
        public new static KnownProjectFileType Instance  { get; private set; }

        private SpringProjectFileType() : base(Name, "Spring", new[] {Spring_EXTENSION})
        {
        }

        protected SpringProjectFileType(string name) : base(name)
        {
        }

        protected SpringProjectFileType(string name, string presentableName) : base(name, presentableName)
        {
        }

        protected SpringProjectFileType(string name, string presentableName, IEnumerable<string> extensions) : base(name,
            presentableName, extensions)
        {
        }

        public const string Spring_EXTENSION = ".pas";
    }
}
