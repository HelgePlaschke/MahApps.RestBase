namespace MahApps.RESTBase
{
    public abstract class RestMethodsBase<T>
    {
        public T Context { get; set; }
        public RestMethodsBase(T Context)
        {
            this.Context = Context;
        }
    }
}
