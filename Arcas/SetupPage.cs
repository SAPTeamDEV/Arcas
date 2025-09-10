using System.Windows.Forms;

namespace Arcas
{
    public abstract class SetupPage
    {
        public abstract string Title { get; }
        public abstract string Subtitle { get; }
        public virtual bool CanGoBack => true;
        public virtual bool CanGoNext => true;

        public abstract Control CreateContent();
        public virtual bool ValidatePage() => true;
    }
}