# ZArchitecture 72.3

Fix compiler CS0246: EasyAuthPrincipalPayload and EasyAuthPrincipalClaim were file-scoped in EasyEntraHeaderPrincipalExtensions.cs, while HeaderOrClaimActorProvider.cs requires access. Moved each into an internal record in its own C# file. No JSON contract or behavior changes.
