namespace MahApps.RESTBase
{
    public abstract class RestMethodsBase<T>
    {
        public RestMethodsBase(T context)
        {
            Context = context;
        }

        public T Context { get; set; }
    }
}