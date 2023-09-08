namespace RR.FakeCosmosEasy.Helpers
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class PredicateFunctionAttribute : Attribute
    {
        public string Name { get; }

        public PredicateFunctionAttribute(string name)
        {
            Name = name;
        }
    }
}
