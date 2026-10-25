using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.ForestCharacters
{
	public sealed class ForestBoneMotion
	{
		private const float SpringLimit = 40f;
		private const float StiffRoot = 2600f;
		private const float StiffTip = 700f;
		private const float StiffDepthRange = 4f;
		private const float FollowDamping = 0.62f;
		private const int FollowSubsteps = 4;
		private const float SwayDamping = 0.55f;
		private const int SwaySubsteps = 3;
		private const float SwayPerVelocity = 0.025f;
		private const float SwayLimit = 22f;
		private const float MinSpringStep = 1e-4f;
		private const float MaxSpringStep = 1f / 30f;

		private ForestCharacterModel _model;
		private float[] _angles;
		private Vector2[] _scales;
		private float[] _world;
		private float[] _springAngles;
		private float[] _springVelocities;
		private bool[] _springReady;
		private float[] _shownAngles;
		private Vector2[] _shownScales;
		private float[] _mixAngles;
		private Vector2[] _mixScales;
		private Vector2 _shownRootOffset;
		private Vector2 _shownRootScale = Vector2.one;
		private float _shownRootRotation;
		private Vector2 _mixRootOffset;
		private Vector2 _mixRootScale = Vector2.one;
		private float _mixRootRotation;

		public IReadOnlyList<float> Angles => _angles;
		public IReadOnlyList<Vector2> Scales => _scales;
		public Vector2 RootOffset { get; private set; }
		public Vector2 RootScale { get; private set; } = Vector2.one;
		public float RootRotation { get; private set; }

		public void Reset(ForestCharacterModel model)
		{
			_model = model;
			var count = model.BoneCount;
			_angles = new float[count];
			_scales = new Vector2[count];
			_world = new float[count];
			_springAngles = new float[count];
			_springVelocities = new float[count];
			_springReady = new bool[count];
			_shownAngles = new float[count];
			_shownScales = FilledScales(count);
			_mixAngles = new float[count];
			_mixScales = FilledScales(count);
			_shownRootOffset = _mixRootOffset = Vector2.zero;
			_shownRootScale = _mixRootScale = Vector2.one;
			_shownRootRotation = _mixRootRotation = 0f;
		}

		public void StartMix()
		{
			Array.Copy(_shownAngles, _mixAngles, _shownAngles.Length);
			Array.Copy(_shownScales, _mixScales, _shownScales.Length);
			_mixRootOffset = _shownRootOffset;
			_mixRootScale = _shownRootScale;
			_mixRootRotation = _shownRootRotation;
		}

		public void Evaluate(ForestClip clip, float time, float mixWeight, float deltaTime, float forwardVelocity, float overlap)
		{
			SampleBones(clip, time);
			SampleRoot(clip, time);
			BlendFromMix(mixWeight);
			RememberShown();
			ApplySprings(Mathf.Clamp(deltaTime, MinSpringStep, MaxSpringStep), forwardVelocity, overlap);
		}

		private Vector2[] FilledScales(int count)
		{
			var scales = new Vector2[count];
			for (var i = 0; i < count; i++)
				scales[i] = Vector2.one;
			return scales;
		}

		private void SampleBones(ForestClip clip, float time)
		{
			for (var bone = 0; bone < _angles.Length; bone++)
			{
				_angles[bone] = clip?.BoneRotations[bone]?.Sample(time) ?? 0f;
				_scales[bone] = new Vector2(clip?.BoneScalesX[bone]?.Sample(time) ?? 1f, clip?.BoneScalesY[bone]?.Sample(time) ?? 1f);
			}
		}

		private void SampleRoot(ForestClip clip, float time)
		{
			RootOffset = new Vector2(clip?.RootX?.Sample(time) ?? 0f, clip?.RootY?.Sample(time) ?? 0f);
			RootScale = new Vector2(clip?.RootScaleX?.Sample(time) ?? 1f, clip?.RootScaleY?.Sample(time) ?? 1f);
			RootRotation = clip?.RootRotation?.Sample(time) ?? 0f;
		}

		private void BlendFromMix(float weight)
		{
			if (weight >= 1f)
				return;

			for (var bone = 0; bone < _angles.Length; bone++)
			{
				_angles[bone] = Mathf.Lerp(_mixAngles[bone], _angles[bone], weight);
				_scales[bone] = Vector2.Lerp(_mixScales[bone], _scales[bone], weight);
			}

			RootOffset = Vector2.Lerp(_mixRootOffset, RootOffset, weight);
			RootScale = Vector2.Lerp(_mixRootScale, RootScale, weight);
			RootRotation = Mathf.Lerp(_mixRootRotation, RootRotation, weight);
		}

		private void RememberShown()
		{
			Array.Copy(_angles, _shownAngles, _angles.Length);
			Array.Copy(_scales, _shownScales, _scales.Length);
			_shownRootOffset = RootOffset;
			_shownRootScale = RootScale;
			_shownRootRotation = RootRotation;
		}

		private void ApplySprings(float deltaTime, float forwardVelocity, float overlap)
		{
			for (var bone = 0; bone < _angles.Length; bone++)
			{
				var parent = _model.Parents[bone];
				var parentWorld = parent >= 0 ? _world[parent] : 0f;
				var target = parentWorld + _angles[bone];

				if (_model.FollowsThrough[bone] && overlap > 0f)
					target = Follow(bone, target, deltaTime, overlap);
				else if (_model.Springs[bone] > 0f)
					target = Sway(bone, target, deltaTime, forwardVelocity);

				_world[bone] = target;
				_angles[bone] = target - parentWorld;
			}
		}

		private float Follow(int bone, float target, float deltaTime, float overlap)
		{
			var depth = Mathf.Clamp01((_model.Depths[bone] - 1) / StiffDepthRange);
			var stiffness = Mathf.Lerp(StiffRoot, StiffTip, depth) / overlap;
			var angle = Spring(bone, target, stiffness, FollowDamping, FollowSubsteps, deltaTime);
			if (Mathf.Abs(angle - target) > SpringLimit)
				angle = target + Mathf.Sign(angle - target) * SpringLimit;

			_springAngles[bone] = angle;
			return angle;
		}

		private float Sway(int bone, float target, float deltaTime, float forwardVelocity)
		{
			target += Mathf.Clamp(forwardVelocity * SwayPerVelocity, -SwayLimit, SwayLimit);
			return Spring(bone, target, _model.Springs[bone], SwayDamping, SwaySubsteps, deltaTime);
		}

		private float Spring(int bone, float target, float stiffness, float dampingRatio, int substeps, float deltaTime)
		{
			if (!_springReady[bone])
			{
				_springReady[bone] = true;
				_springAngles[bone] = target;
				_springVelocities[bone] = 0f;
			}

			var angle = _springAngles[bone];
			var velocity = _springVelocities[bone];
			var damping = 2f * Mathf.Sqrt(stiffness) * dampingRatio;
			var step = deltaTime / substeps;
			for (var i = 0; i < substeps; i++)
			{
				velocity += ((target - angle) * stiffness - velocity * damping) * step;
				angle += velocity * step;
			}

			_springAngles[bone] = angle;
			_springVelocities[bone] = velocity;
			return angle;
		}
	}
}
