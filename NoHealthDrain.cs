using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Reflection;

namespace NoHealthDrain
{
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("TheCoinGame.exe")]
    public class NoHealthDrain : BaseUnityPlugin
    {
        private const string ModId = "pykess.thecoingame.plugins.nohealthdrain";
        private const string ModName = "No Health Drain";
        public const string Version = "0.0.0";
        private string CompatibilityModName => ModName.Replace(" ", "");

        public static NoHealthDrain instance;

        private Harmony harmony;

#if DEBUG
        public static readonly bool DEBUG = true;
#else
        public static readonly bool DEBUG = false;
#endif
        internal static void Log(string str)
        {
            if (DEBUG)
            {
                UnityEngine.Debug.Log($"[{ModName}] {str}");
            }
        }


        private void Awake()
        {
            instance = this;
            
            harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
        private void Start()
        {


        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        internal static string GetConfigKey(string key) => $"{NoHealthDrain.ModName}_{key}";


    }
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    class PlayMakerFSM_Patch_Start
    {
        static void Postfix(PlayMakerFSM __instance)
        {
            __instance.gameObject.AddComponent<PreventHealthDrain>();
        }
    }
    class PreventHealthDrain : MonoBehaviour
    {
        /// <summary>
        /// force the subtract action of the "DYING" state of the health controller to subtract 0
        /// since the game is made with PlayMaker, the only way to do this is to patch EVERY instance of PlayMakerFSM,
        ///     then check if the FSM is on the object we want, then overwrite the entire object value that is being subtracted
        ///     with a brand new FsmFloat (so that it's not overwritten with the original value) and set its value to something
        ///     very small, but nonzero. it cannot be 0 because then the energy display would stop updating...
        ///     
        ///     this is terrible. but PlayMakerFSM is worse.

        private string MATCHINGOBJ = "HEALTH CNTRL";
        private string MATCHINGSTATE = "DYING";

        private PlayMakerFSM fsm_backingfield;
        private PlayMakerFSM fsm
        {
            get
            {
                if (this.fsm_backingfield is null)
                {
                    this.fsm_backingfield = this.gameObject.GetComponent<PlayMakerFSM>();
                }
                return this.fsm_backingfield;
            }
        }

        private FsmState dyingstate_backingfield;
        private FsmState dyingstate
        {
            get
            {
                if (this.dyingstate_backingfield is null)
                {
                    this.dyingstate_backingfield = Array.Find<FsmState>(this.fsm.FsmStates, x => x.Name == this.MATCHINGSTATE);
                }
                return this.dyingstate_backingfield;
            }
        }

        private FloatSubtract subtract_backingfield;
        private FloatSubtract subtract
        {
            get
            {
                if (this.subtract_backingfield is null)
                {
                    this.subtract_backingfield = (FloatSubtract) Array.Find<FsmStateAction>(this.dyingstate.Actions, x => x is FloatSubtract);
                }
                return this.subtract_backingfield;
            }
        }

        void Start()
        {
            // destroy this behavior if not on the correct object, or the behavior is already disabled or null, or the state/action is null
            if (!this.gameObject.name.Contains(this.MATCHINGOBJ) || !(this.fsm?.enabled ?? false) || this.dyingstate is null || this.subtract is null)
            {
                Destroy(this);
            }

            this.subtract.subtract = new FsmFloat();
            // set subtract to some very small number so the display still updates
            this.subtract.subtract = 0.0001f; // any smaller than 0.0001 and PlayMaker forces the value to 0...
        }
    }
}
