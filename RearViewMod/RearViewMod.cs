using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace RearViewMod
{
    public class RearViewMod : ModBehaviour
    {
        public static RearViewMod Instance;

        public OWCamera owCamera;
        GameObject promptCanvas;
        RawImage img;
        Image rearviewReticle;
        bool isRearViewToggled = false;

        KeyControl rearViewKey;
        bool rearViewToggle;
        bool mirrorRearViewCamera;
        bool moveRearViewCamera;

        int rearViewCameraX;
        int rearViewCameraY;
        int rearViewCameraWidth;
        int rearViewCameraHeight;

        public void Awake()
        {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        public void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(RearViewMod)} is loaded!", MessageType.Success);

            new Harmony("MDJ287.RearViewMod").PatchAll(Assembly.GetExecutingAssembly());

            // Example of accessing game code.
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        public override void Configure(IModConfig config)
        {
            rearViewKey = Keyboard.current.FindKeyOnCurrentKeyboardLayout(config.GetSettingsValue<string>("rearViewKey"));
            rearViewToggle = config.GetSettingsValue<bool>("rearViewToggle");
            mirrorRearViewCamera = config.GetSettingsValue<bool>("mirrorRearViewCamera");
            moveRearViewCamera = config.GetSettingsValue<bool>("moveRearViewCamera");

            rearViewCameraX = config.GetSettingsValue<int>("rearViewCameraX");
            rearViewCameraY = config.GetSettingsValue<int>("rearViewCameraY");
            rearViewCameraWidth = config.GetSettingsValue<int>("rearViewCameraWidth");
            rearViewCameraHeight = config.GetSettingsValue<int>("rearViewCameraHeight");

            if (!rearViewToggle) isRearViewToggled = false;

            Destroy(img);
            Destroy(rearviewReticle);

            if (isRearViewToggled)
            {
                CreateRearViewMirror();
            }
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            switch (newScene)
            {
                case OWScene.SolarSystem:
                case OWScene.EyeOfTheUniverse:
                    promptCanvas = GameObject.Find("ScreenPromptCanvas");
                    break;
            }
        }

        private void CreateRearViewMirror()
        {
            owCamera.aspect = rearViewCameraWidth / rearViewCameraHeight;
            owCamera.targetTexture = new RenderTexture(rearViewCameraWidth, rearViewCameraHeight, 16);

            owCamera.targetTexture.Create();
            GameObject imgGameObj = new();
            imgGameObj.transform.SetParent(promptCanvas.transform);
            img = imgGameObj.AddComponent<RawImage>();
            img.texture = owCamera.targetTexture;
            owCamera.targetTexture.Release();
            // most of this probably doesn't do anything
            img.rectTransform.anchorMin = new(0, 0);
            img.rectTransform.anchorMax = new(0, 0);
            img.rectTransform.pivot = new(0, 0);
            img.rectTransform.offsetMin = new(0, 0);
            img.rectTransform.offsetMax = new(rearViewCameraWidth, rearViewCameraHeight);
            img.transform.position = new(rearViewCameraX, rearViewCameraY, 0);
            if (mirrorRearViewCamera)
            {
                img.transform.position += new Vector3(rearViewCameraWidth, 0, 0);
                img.transform.localScale = new(-img.transform.localScale.x, img.transform.localScale.y, img.transform.localScale.z);
            }

            GameObject rearviewReticleGameObj = new();
            rearviewReticleGameObj.transform.SetParent(promptCanvas.transform);
            rearviewReticle = rearviewReticleGameObj.AddComponent<Image>();
            rearviewReticle.sprite = FindObjectOfType<ReticleController>()._defaultReticle;
            rearviewReticle.rectTransform.anchorMin = new(0, 0);
            rearviewReticle.rectTransform.anchorMax = new(0, 0);
            rearviewReticle.rectTransform.pivot = new(0, 0);
            rearviewReticle.rectTransform.offsetMin = new(0, 0);
            rearviewReticle.rectTransform.offsetMax = new(16, 16);
            rearviewReticle.transform.position = new(rearViewCameraX + rearViewCameraWidth / 2 - 8, rearViewCameraY + rearViewCameraHeight / 2 - 8);
        }

        public void Update()
        {
            /*
             * LOOK AT:
             * PlayerCameraController
            */
            // set from player settings?
            if (Locator._playerCamera == null || Locator._playerBody == null)
            {
                return;
            }

            if (owCamera == null)
            {
                GameObject cameraGameObj = new();
                cameraGameObj.transform.SetParent(Locator._playerBody.transform);
                owCamera = cameraGameObj.AddComponent<OWCamera>();
                owCamera.mainCamera.CopyFrom(Locator._playerCamera.mainCamera);
                owCamera.aspect = rearViewCameraWidth / rearViewCameraHeight;
                owCamera.targetTexture = new RenderTexture(rearViewCameraWidth, rearViewCameraHeight, 16);
                if (isRearViewToggled)
                {
                    CreateRearViewMirror();
                }
            }

            if ((!rearViewToggle && rearViewKey.isPressed) || isRearViewToggled)
            {
                owCamera.transform.SetPositionAndRotation(Locator._playerCamera.transform.position, Locator._playerCamera.transform.rotation);
                owCamera.transform.localRotation *= Quaternion.Euler(0, 180, 0);
                if (moveRearViewCamera)
                {
                    owCamera.transform.position += owCamera.transform.forward * 2; // move 2m behind player so the body doesn't get in the way
                }
            }
            if (rearViewKey.wasPressedThisFrame)
            {
                if (rearViewToggle)
                {
                    isRearViewToggled = !isRearViewToggled;
                }
                if (!rearViewToggle || isRearViewToggled)
                {
                    CreateRearViewMirror();
                }
            }
            if (!rearViewToggle ? rearViewKey.wasReleasedThisFrame : (!isRearViewToggled && rearViewKey.wasPressedThisFrame))
            {
                Destroy(img);
                Destroy(rearviewReticle);
            }
        }
    }

}
