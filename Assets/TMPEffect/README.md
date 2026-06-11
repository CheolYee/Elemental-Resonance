# TMPEffect

## Getting Started

1. **Add the Component**:  
   - Select your `TMP_Text` object in the Unity Editor.  
   - Click the `Add Component` button and attach the `TMPEffect` script.  

2. **Configure Effects**:  
   - Adjust **Outline** and **Shadow** parameters directly in the Inspector.  
   - No need to manually manage materials.  

✅ **Key Advantage**: Automatically handles TMP material. Texts with identical effects can leverage **Dynamic Batching**.  

---

## Examples

The `TMPEffectExample` scene demonstrates the following effects:  
- Basic Outline  
- Double Outline  
- Shadow  

📍 **Location**: Find the example scene in `Assets/TMPEffect/Examples`.  

---

## Creating Custom TMPEffects

### Inherit the Base Class TMPEffectBase
```csharp
public class CustomEffect : TMPEffectBase 
{
    protected override void DoSetMaterial(Material mat)
    {
        // Implement your custom material logic here
        // Set material properties to get custom effect
    }
}
```