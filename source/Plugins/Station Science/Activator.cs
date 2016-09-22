using StationScience;
using System;
using System.Collections.Generic;

namespace KerboKatz.ASS.SS
{
  internal class Activator : IScienceActivator
  {
    private AutomatedScienceSampler _AutomatedScienceSamplerInstance;

    AutomatedScienceSampler IScienceActivator.AutomatedScienceSampler
    {
      get { return _AutomatedScienceSamplerInstance; }
      set { _AutomatedScienceSamplerInstance = value; }
    }

    public bool CanRunExperiment(ModuleScienceExperiment baseExperiment, float currentScienceValue)
    {
      var currentExperiment = baseExperiment as StationExperiment;
      var isActive = currentExperiment.Events["StartExperiment"].active;
      if (isActive)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": StationExperiment didn't start yet! You might want to start it manually!");
        return false;
      }
      if (StationExperiment.checkBoring(FlightGlobals.ActiveVessel, false))
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": StationExperiment says this location is boring!");
        return false;
      }
      if (!currentExperiment.finished() && !isActive)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": StationExperiment isn't finished yet!");
        return false;
      }
      if (!currentExperiment.experiment.IsAvailableWhile(ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel), FlightGlobals.currentMainBody))//
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment isn't available in the current situation: ", ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel), "_", FlightGlobals.currentMainBody + "_", currentExperiment.experiment.situationMask);
        return false;
      }
      if (currentExperiment.Inoperable)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment is inoperable");
        return false;
      }
      if (currentExperiment.Deployed)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment is deployed");
        return false;
      }

      if (!currentExperiment.rerunnable && !_AutomatedScienceSamplerInstance.craftSettings.oneTimeOnly)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Runing rerunable experiments is disabled");
        return false;
      }
      if (currentScienceValue < _AutomatedScienceSamplerInstance.craftSettings.threshold)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Science value is less than cutoff threshold: ", currentScienceValue, "<", _AutomatedScienceSamplerInstance.craftSettings.threshold);
        return false;
      }
      if (!currentExperiment.experiment.IsUnlocked())
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment is locked");
        return false;
      }
      return true;
    }

    public void DeployExperiment(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as StationExperiment;
      if (_AutomatedScienceSamplerInstance.craftSettings.hideScienceDialog)
      {
        var stagingSetting = currentExperiment.useStaging;
        currentExperiment.useStaging = true;//work the way around the staging
        currentExperiment.OnActive();//run the experiment without causing the report to show up
        currentExperiment.useStaging = stagingSetting;//set the staging back
      }
      else
      {
        currentExperiment.DeployExperiment();
      }
    }

    public ScienceSubject GetScienceSubject(ModuleScienceExperiment baseExperiment)
    {
      //experiment.BiomeIsRelevantWhile
      return ResearchAndDevelopment.GetExperimentSubject(baseExperiment.experiment, ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel), FlightGlobals.currentMainBody, CurrentBiome(baseExperiment.experiment));
    }

    public float GetScienceValue(ModuleScienceExperiment baseExperiment, Dictionary<string, int> shipCotainsExperiments, ScienceSubject currentScienceSubject)
    {
      return Utilities.Science.GetScienceValue(shipCotainsExperiments, baseExperiment.experiment, currentScienceSubject);
    }

    public bool CanReset(ModuleScienceExperiment baseExperiment)
    {
      if (!baseExperiment.Inoperable)
      {
        _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Experiment isn't inoperable");
        return false;
      }
      if (!baseExperiment.Deployed)
      {
        _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Experiment isn't deployed!");
        return false;
      }
      if (baseExperiment.GetScienceCount() > 0)
      {
        _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Experiment has data!");
        return false;
      }

      if (!baseExperiment.resettable)
      {
        _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Experiment isn't resetable");
        return false;
      }
      bool hasScientist = false;
      foreach (var crew in FlightGlobals.ActiveVessel.GetVesselCrew())
      {
        if (crew.trait == "Scientist")
        {
          hasScientist = true;
          break;
        }
      }
      if (!hasScientist)
      {
        _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Vessel has no scientist");
        return false;
      }
      _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Can reset");
      return true;
    }

    public void Reset(ModuleScienceExperiment baseExperiment)
    {
      _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Reseting experiment");
      baseExperiment.ResetExperiment();
    }

    public bool CanTransfer(ModuleScienceExperiment baseExperiment, ModuleScienceContainer moduleScienceContainer)
    {
      if (baseExperiment.GetScienceCount() == 0)
      {
        _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Experiment has no data skiping transfer ", baseExperiment.GetScienceCount());
        return false;
      }
      if (!baseExperiment.IsRerunnable())
      {
        if (!_AutomatedScienceSamplerInstance.craftSettings.transferAllData)
        {
          _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Experiment isn't rerunnable and transferAllData is turned off.");
          return false;
        }
      }
      if (!_AutomatedScienceSamplerInstance.craftSettings.dumpDuplicates)
      {
        foreach (var data in baseExperiment.GetData())
        {
          if (moduleScienceContainer.HasData(data))
          {
            _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": Target already has experiment and dumping is disabled.");
            return false;
          }
        }
      }
      _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": We can transfer the science!");
      return true;
    }

    public void Transfer(ModuleScienceExperiment baseExperiment, ModuleScienceContainer moduleScienceContainer)
    {
      _AutomatedScienceSamplerInstance.Log(baseExperiment.experimentID, ": transfering");
      moduleScienceContainer.StoreData(new List<IScienceDataContainer>() { baseExperiment }, _AutomatedScienceSamplerInstance.craftSettings.dumpDuplicates);
    }

    private string CurrentBiome(ScienceExperiment baseExperiment)
    {
      if (!baseExperiment.BiomeIsRelevantWhile(ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel)))
        return string.Empty;
      var currentVessel = FlightGlobals.ActiveVessel;
      var currentBody = FlightGlobals.currentMainBody;
      if (currentVessel != null && currentBody != null)
      {
        if (!string.IsNullOrEmpty(currentVessel.landedAt))
        {
          //big thanks to xEvilReeperx for this one.
          return Vessel.GetLandedAtString(currentVessel.landedAt);
        }
        else
        {
          return ScienceUtil.GetExperimentBiome(currentBody, currentVessel.latitude, currentVessel.longitude);
        }
      }
      else
      {
        _AutomatedScienceSamplerInstance.Log("currentVessel && currentBody == null");
      }
      return string.Empty;
    }

    public List<Type> GetValidTypes()
    {
      var types = new List<Type>();
      types.Add(typeof(StationExperiment));

      Utilities.LoopTroughAssemblies((type) =>
      {
        if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(StationExperiment)))
        {
          types.Add(type);
        }
      });
      return types;
    }
  }
}