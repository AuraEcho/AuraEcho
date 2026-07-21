using Prism.Mvvm;
using Prism.Navigation;

namespace AuraEcho.Toolkit.Wpf.Mvvm
{
    public abstract class ViewModelBase : BindableBase, IDestructible
    {
        protected ViewModelBase()
        {

        }

        public virtual void Destroy()
        {

        }
    }
}
