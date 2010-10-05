namespace MahApps.RESTBase
{
    public abstract class RestMethodsBase<T>
    {
        public RestMethodsBase(T Context)
        {
            this.Context = Context;
        }

        public T Context { get; set; }
    }
}