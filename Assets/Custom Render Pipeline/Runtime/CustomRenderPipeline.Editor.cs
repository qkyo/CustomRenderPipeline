/*
 * @Author: Qkyo
 * @Date: 2023-01-14 14:04:40
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-30 16:56:21
 * @FilePath: \CustomRenderPipeline\Assets\Custom Render Pipeline\Runtime\CustomRenderPipeline.Editor.cs
 * @Description: 
 */

using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

public partial class CustomRenderPipeline {
	
	partial void InitializeForEditor();

	// For the editor only, we have to override how the lightmapper sets up its light data.
	// So that to use override light falloff when baked the light.
	// The default lighht in legacy RP is too bright to use.
	#if UNITY_EDITOR
		partial void InitializeForEditor () {
			Lightmapping.SetDelegate(lightsDelegate);
		}

		// Clean up and reset the delegate when our pipeline gets disposed
		protected override void Dispose (bool disposing) {
			base.Dispose(disposing);
			Lightmapping.ResetDelegate();
		}

		// Providing it with a delegate to a method that 
		// transfers data from an input Light array to a NativeArray<LightDataGI> output.
		static Lightmapping.RequestLightsDelegate lightsDelegate =
		(Light[] lights, NativeArray<LightDataGI> output) => {
			var lightData = new LightDataGI();
			for (int i = 0; i < lights.Length; i++) {
				Light light = lights[i];
				switch (light.type) {
					case LightType.Directional:
						var directionalLight = new DirectionalLight();
						LightmapperUtils.Extract(light, ref directionalLight);
						lightData.Init(ref directionalLight);
						break;
					case LightType.Point:
						var pointLight = new PointLight();
						LightmapperUtils.Extract(light, ref pointLight);
						lightData.Init(ref pointLight);
						break;
					case LightType.Spot:
						var spotLight = new SpotLight();
						LightmapperUtils.Extract(light, ref spotLight);
						spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
						spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
						lightData.Init(ref spotLight);
						break;
					case LightType.Area:
						var rectangleLight = new RectangleLight();
						LightmapperUtils.Extract(light, ref rectangleLight);
						// We don't support realtime area lights, 
						// so let's force their light mode to baked if they exist.
						rectangleLight.mode = LightMode.Baked;
						lightData.Init(ref rectangleLight);
						break;
					default:
						lightData.InitNoBake(light.GetInstanceID());
						break;
				}
				// We can now set the falloff type of the light data
				lightData.falloff = FalloffType.InverseSquared;
				output[i] = lightData;
			}
		};
	#endif
	
}