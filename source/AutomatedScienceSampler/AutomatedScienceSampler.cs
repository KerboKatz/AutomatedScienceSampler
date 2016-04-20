using KerboKatz.Assets;
using KerboKatz.Extensions;
using KerboKatz.Toolbar;
using KSP.UI;
using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KerboKatz.ASS
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class AutomatedScienceSampler : KerboKatzBase<Settings>, IToolbar
  {
    private Dictionary<string, int> shipCotainsExperiments = new Dictionary<string, int>();
    private Dictionary<Type, IScienceActivator> activators = new Dictionary<Type, IScienceActivator>();
    private double nextUpdate;
    private float CurrentFrame;
    private float lastFrameCheck;
    private static List<GameScenes> _activeScences = new List<GameScenes>() { GameScenes.FLIGHT };
    private Sprite _icon = AssetLoader.GetAsset<Sprite>("icon56", "Icons/AutomatedScienceSampler");//Utilities.GetTexture("icon56", "AutomatedScienceSampler/Textures");
    private int currentSelectedContainer;
    private List<ModuleScienceExperiment> experiments;
    private List<ModuleScienceContainer> scienceContainers;
    private Dropdown transferScienceUIElement;
    private bool isReady;
    string settingsUIName;

    private float frameCheck { get { return 1 / settings.spriteFPS; } }
    #region init/destroy
    public AutomatedScienceSampler()
    {
      modName = "AutomatedScienceSampler";
      displayName = "Automated Science Sampler";
      settingsUIName = "AutomatedScienceSampler";
      tooltip = "Use left click to turn AutomatedScienceSampler on/off.\n Use shift+left click to open the settings menu.";
      requiresUtilities = new Version(1, 3, 0);
      ToolbarBase.instance.Add(this);
      LoadSettings("AutomatedScienceSampler", "Settings");
      Log("Init done!");
    }


    public override void OnAwake()
    {
      if (!(HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
      {
        Log("Game mode not supported!");
        Destroy(this);
        return;
      }
      GetScienceActivators();
      LoadUI(settingsUIName);
      Log("Awake");
    }
    private void GetScienceActivators()
    {
      var instances = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                      from t in assembly.GetTypes()
                      where t.GetInterfaces().Contains(typeof(IScienceActivator))
                              && t.GetConstructor(Type.EmptyTypes) != null
                      select Activator.CreateInstance(t) as IScienceActivator;
      Log("Starting search");
      foreach (var activator in instances)
      {
        try
        {
          activator.AutomatedScienceSampler = this;
          Log("Found" , activator.GetType());
          foreach (var type in activator.GetValidTypes())
          {
            Log("...for type: " , type);
            activators.Add(type, activator);
          }
        }
        catch (Exception e)
        {
          Log("Failed to initialize activator. If you aren't using one of the plugins this message is harmless!\n" , e);
        }
      }
      DefaultActivator.instance = activators[typeof(ModuleScienceExperiment)] as DefaultActivator;
      GameEvents.onVesselChange.Add(OnVesselChange);
    }

    protected override void AfterDestroy()
    {
      GameEvents.onVesselChange.Remove(OnVesselChange);
      ToolbarBase.instance.Remove(this);
      Log("AfterDestroy");
    }
    #endregion
    #region ui
    protected override void OnUIElemntInit(UIData uiWindow)
    {
      var prefabWindow = uiWindow.gameObject.transform as RectTransform;
      var content = prefabWindow.FindChild("Content");

      InitInputField(content, "Threshold", settings.threshold.ToString(), OnThresholdChange);
      InitToggle(content, "SingleRunExperiments", settings.oneTimeOnly, OnSingleRunExperimentsChange);
      InitToggle(content, "ResetExperiments", settings.resetExperiments, OnResetExperimentsChange);
      InitToggle(content, "HideScienceDialog", settings.hideScienceDialog, OnHideScienceDialogChange);
      InitToggle(content, "TransferAllData", settings.transferAllData, OnTransferAllDataChange);
      InitToggle(content, "DumpDuplicates", settings.dumpDuplicates, OnDumpDuplicatesChange);
      InitToggle(content, "Debug", settings.debug, OnDebugChange);
      InitSlider(content, "SpriteFPS", settings.spriteFPS, OnSpriteFPSChange);
      transferScienceUIElement = InitDropdown(content, "TransferScience", OnTransferScienceChange);

    }

    private void OnDumpDuplicatesChange(bool arg0)
    {
      settings.dumpDuplicates = arg0;
      settings.Save();
      Log("OnDumpDuplicatesChange");
    }

    private void OnTransferAllDataChange(bool arg0)
    {
      settings.transferAllData = arg0;
      settings.Save();
      Log("OnTransferAllDataChange");
    }

    private void OnSpriteFPSChange(float arg0)
    {
      settings.spriteFPS = arg0;
      settings.Save();
      Log("OnSpriteFPSChange");
    }

    private void OnTransferScienceChange(int arg0)
    {
      currentSelectedContainer = arg0;
      Log("OnTransferScienceChange");
    }

    private void OnDebugChange(bool arg0)
    {
      settings.debug = arg0;
      settings.Save();
      Log("OnDebugChange");
    }

    private void OnHideScienceDialogChange(bool arg0)
    {
      settings.hideScienceDialog = arg0;
      settings.Save();
      Log("OnHideScienceDialog");
    }

    private void OnResetExperimentsChange(bool arg0)
    {
      settings.resetExperiments = arg0;
      settings.Save();
      Log("OnResetExperimentsChange");
    }

    private void OnSingleRunExperimentsChange(bool arg0)
    {
      settings.oneTimeOnly = arg0;
      settings.Save();
      Log("onSingleRunExperimentsChange");
    }

    private void OnThresholdChange(string arg0)
    {
      settings.threshold = arg0.ToFloat();
      settings.Save();
      Log("onThresholdChange");
    }
    private void OnToolbar()
    {
      if (Input.GetMouseButtonUp(1))
      {
        var uiData = GetUIData(settingsUIName);
        if (uiData == null || uiData.canvasGroup == null)
          return;
        settings.showSettings = !settings.showSettings;
        if (settings.showSettings)
        {
          FadeCanvasGroup(uiData.canvasGroup, 1, settings.uiFadeSpeed);
        }
        else
        {
          FadeCanvasGroup(uiData.canvasGroup, 0, settings.uiFadeSpeed);
        }
      }
      else
      {
        settings.runAutoScience = !settings.runAutoScience;
        if (!settings.runAutoScience)
        {
          icon = AssetLoader.GetAsset<Sprite>("icon56", "Icons/AutomatedScienceSampler");//Utilities.GetTexture("icon56", "AutomatedScienceSampler/Textures");
        }
      }
      settings.Save();
    }
    #endregion

    private void OnVesselChange(Vessel data)
    {
      UpdateShipInformation();
    }

    void Update()
    {
      //Debug.Log(transferScienceUIElement.options.Count);
      if (!settings.runAutoScience)
        return;
      if (!isReady)
        return;
      #region icon
      if (lastFrameCheck + frameCheck < Time.time)
      {
        var frame = Time.deltaTime / frameCheck;
        if (CurrentFrame + frame < 55)
          CurrentFrame += frame;
        else
          CurrentFrame = 0;
        icon = AssetLoader.GetAsset<Sprite>("icon" + (int)CurrentFrame, "Icons/AutomatedScienceSampler");//Utilities.GetTexture("icon" + (int)CurrentFrame, "ForScienceContinued/Textures");
        lastFrameCheck = Time.time;
      }

      #endregion
      var sw = new System.Diagnostics.Stopwatch();
      sw.Start();
      if (nextUpdate == 0)
      {//add some delay so it doesnt run as soon as the vehicle launches
        if (!FlightGlobals.ready)
          return;
        nextUpdate = Planetarium.GetUniversalTime() + 1;
        UpdateShipInformation();
        return;
      }

      if (!FlightGlobals.ready || !Utilities.canVesselBeControlled(FlightGlobals.ActiveVessel))
        return;
      if (Planetarium.GetUniversalTime() < nextUpdate)
        return;
      nextUpdate = Planetarium.GetUniversalTime() + 1;
      Log(sw.Elapsed.TotalMilliseconds);
      //var experiments = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
      Log(sw.Elapsed.TotalMilliseconds);
      foreach (var experiment in experiments)
      {
        IScienceActivator activator;
        if (!activators.TryGetValue(experiment.GetType(), out activator))
        {
          Log("Activator for " , experiment.GetType() , " not found! Using default!");
          activator = DefaultActivator.instance;
        }
        var subject = activator.GetScienceSubject(experiment.experiment);
        var value = activator.GetScienceValue(experiment.experiment, shipCotainsExperiments, subject);
        if (activator.CanRunExperiment(experiment, value))
        {
          Log("Deploying " , experiment.part.name , " for :" ,value , " science! " , subject.id);
          activator.DeployExperiment(experiment);
          AddToContainer(subject.id);
        }
        else if (settings.resetExperiments && activator.CanReset(experiment))
        {
          activator.Reset(experiment);
        }
        else if (currentSelectedContainer != 0 && scienceContainers.Count <= currentSelectedContainer && activator.CanTransfer(experiment, scienceContainers[currentSelectedContainer - 1]))
        {
          activator.Transfer(experiment, scienceContainers[currentSelectedContainer - 1]);
        }
        Log(sw.Elapsed.TotalMilliseconds);
      }
      Log("Total: " , sw.Elapsed.TotalMilliseconds);
    }

    private void UpdateShipInformation()
    {
      isReady = false;

      while (transferScienceUIElement.options.Count > 1)
      {
        transferScienceUIElement.options.RemoveAt(transferScienceUIElement.options.Count - 1);
      }
      experiments = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
      scienceContainers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
      shipCotainsExperiments.Clear();
      foreach (ModuleScienceContainer currentContainer in scienceContainers)
      {
        AddOptionToDropdown(transferScienceUIElement, currentContainer.part.partInfo.title);
        foreach (var data in currentContainer.GetData())
        {
          AddToContainer(data.subjectID);
        }
      }
      isReady = true;
    }

    private void AddToContainer(string subjectID, int add = 0)
    {
      if (shipCotainsExperiments.ContainsKey(subjectID))
      {
        Log(subjectID, "_Containing");
        shipCotainsExperiments[subjectID] = shipCotainsExperiments[subjectID] + 1 + add;
      }
      else
      {
        Log(subjectID, "_New");
        shipCotainsExperiments.Add(subjectID, add + 1);
      }
    }
    #region IToolbar
    public List<GameScenes> activeScences
    {
      get
      {
        return _activeScences;
      }
    }

    public UnityAction onClick
    {
      get
      {
        return OnToolbar;
      }
    }

    public Sprite icon
    {
      get
      {
        return _icon;
      }
      private set
      {
        if (_icon != value)
        {
          _icon = value;
          ToolbarBase.UpdateIcon();
        }
      }
    }

    #endregion
  }
}