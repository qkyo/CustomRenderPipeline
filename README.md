# Custom Scriptable Render Pipeline

##### Reference
https://catlikecoding.com/unity/tutorials/custom-srp/

##### Unity Version
2021.3.14f1
  
  
### 1. Taking Control of Rendering
 * Create a render pipeline asset and instance. 
 * Render a camera's view. 
 * Perform culling, filtering, and sorting. 
 * Separate opaque, transparent, and invalid passes. 
 * Work with more than one camera.
 
    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Taking%20Control%20of%20Rendering.png)
    
### 2. Draw Calls - Shaders and Batches
 * White a Unlit HLSL shader.
 * Support the SRP batcher, GPU instancing, and dynamic batching.
 * Configure material properties per object and draw many at random.
 * Create transparent and cutout materials.
 
 
    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Draw%20Calls%20Shaders%20and%20Batches.png)
    
### 3. Direct Light - Direct Illumination
 * Use normal vectors to calculate lighting.
 * Support up to four directional lights.
 * Apply the Minimalist CookTorrance BRDF.
 * Make lit transparent materials.
 * Create a custom shader GUI with presets.
 
 
    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Directional%20Light%2C%20BRDF.png)
    
### 4. Directional Shadow - Cascaded Shadow Maps
 * Render and sample shadow maps.
 * Support multiple shadowed directional lights.
 * Use cascaded shadow maps.
 * Blend, fade, and filter shadows.

 
    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Directional%20Shadows%20-%20Cascaded%20Shadow%20Maps.png)


