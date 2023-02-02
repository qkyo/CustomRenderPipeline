# Custom Scriptable Render Pipeline

### Reference
https://catlikecoding.com/unity/tutorials/custom-srp/

### Unity Version
2021.3.14f1
  
### Workflow
#### 1. Taking Control of Rendering
 * Create a render pipeline asset and instance. 
 * Render a camera's view. 
 * Perform culling, filtering, and sorting. 
 * Separate opaque, transparent, and invalid passes. 
 * Work with more than one camera.
 
    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Taking%20Control%20of%20Rendering.png)
    
#### 2. Draw Calls - Shaders and Batches
 * White a Unlit HLSL shader.
 * Support the SRP batcher, GPU instancing, and dynamic batching.
 * Configure material properties per object and draw many at random.
 * Create transparent and cutout materials.
 
    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Draw%20Calls%20Shaders%20and%20Batches.png)
    
#### 3. Direct Light - Direct Illumination
 * Use normal vectors to calculate lighting.
 * Support up to four directional lights.
 * Apply the Minimalist CookTorrance BRDF.
 * Make lit transparent materials.
 * Create a custom shader GUI with presets.
 
    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Directional%20Light%2C%20BRDF.png)
    
#### 4. Directional Shadow - Cascaded Shadow Maps
 * Render and sample shadow maps.
 * Support multiple shadowed directional lights.
 * Use cascaded shadow maps.
 * Blend, fade, and filter shadows.

    ![image](https://github.com/qkyo/CustomRenderPipeline/blob/main/Assets/RenderResultSet/Directional%20Shadows%20-%20Cascaded%20Shadow%20Maps.png)

#### 5. Baked Light - Light Maps and Probes
 * Bake static global illumination.
 * Sample light maps, probes, and LPPVs.
 * Create a meta pass.
 * Support emissive surfaces.

    ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Baked%20Light.png)

#### 6. Shadow Masks - Baking Direct Occlusion
 * Bake static shadows.
 * Combine realtime lighting with baked shadows.
 * Mix realtime and baked shadows.
 * Support up to four shadow mask lights.

    ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Mixed%20realtime%20and%20baked%20shadow.gif)
    ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Mix%20shadow%20-%20Distance%20shadow%20mask%20mode.gif)

#### 7. LOD and Reflections - Adding Details
 * Use LOD Groups.
 * Cross-Fade between LOD levels.
 * Reflect the environment by sampling a reflection probe.
 * Support optional Fresnel reflections.
 
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/LOD%20group.gif)
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Reflection%20probe%2C%20and%20Fresnel%20reflection.png)
     
     
#### 8. Complex Maps - Masks, Details, and Normals
 * Add support for a MODS mask map.
 * Introduce a secondary detail map.
 * Perform tangent-space normal mapping.
 
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/MODS%2C%20Detail%20and%20Normal%20Map.gif)

#### 9. Point and Spot Lights - Lights with Limited Influence
 * Support more light types than only directional.
 * Include realtime point and spot lights.
 * Bake lighting and shadows for point and spot lights.
 * Limit rendering to max 8 other lights per object.
 
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Point%20and%20Spot%20Lights%20and%20Their%20Baked%20Shadow.jpg)
     
#### 10. Point and Spot Shadows - Perspective Shadows
 * Mix baked and realtime shadows for point and spot lights.
 * Add a second shadow atlas.
 * Render and sample shadows with a perspective projection.
 * Use custom cube maps.

     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Spot%20Light%20and%20Point%20Light.jpg)
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/shadow%20acne.jpg)
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Point%20and%20Spot%20Shadows%20-%20Perspective%20Shadows.png)
     
#### 11. Post Processing - Bloom
 * Create a simple post-FX stack.
 * Alter the rendered image.
 * Perform post-processing when needed.
 * Make an **artistic bloom effect**.
 
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Gaussian%20Pyramid.gif) 
     ![image](https://github.com/qkyo/CustomScriptableRenderPipeline/blob/main/Assets/RenderResultSet/Blend%20Result%20-%20bucibic%20filtering%20for%20upsampling.gif)
     


    
