namespace Turnroot.Serialization
{
    /// <summary>
    /// Simple lifecycle interface used by runtime instances to perform repair/initialization
    /// after JSON deserialization (or manual rehydration).
    /// Implementers must use a parameterless constructor or be created by a custom converter.
    /// </summary>
    public interface IPostDeserialize
    {
        void OnAfterDeserialize();
    }
}
