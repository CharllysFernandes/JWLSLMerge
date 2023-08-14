namespace JWLSLMerge.Data.Attributes
{
    /// <summary>
    /// Atributo personalizado para indicar que uma propriedade deve ser ignorada em operações específicas.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute { }
}
