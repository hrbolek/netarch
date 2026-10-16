namespace Architecture.Attributes;

public enum ArchitectureLayer { Model, Database, Security, Service, Resolver, UI, Shared }
public enum ArchitectureRole { Entity, Response, Service, Resolver, Component, Contract, Infrastructure }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false)]
public sealed class ArchitectureAttribute(ArchitectureLayer layer, ArchitectureRole role) : Attribute
{
    public ArchitectureLayer Layer { get; } = layer;
    public ArchitectureRole Role { get; } = role;
}
