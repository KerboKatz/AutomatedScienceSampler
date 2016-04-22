using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerboKatz.ASS
{
  class DefaultActivator : IScienceActivator
  {
    internal static DefaultActivator instance;
    AutomatedScienceSampler _AutomatedScienceSamplerInstance;
    AutomatedScienceSampler IScienceActivator.AutomatedScienceSampler
    {
      get { return _AutomatedScienceSamplerInstance; }
      set { _AutomatedScienceSamplerInstance = value; }
    }

    public bool CanRunExperiment(ModuleScienceExperiment currentExperiment, float currentScienceValue)
    {
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

      if (!currentExperiment.rerunnable && !_AutomatedScienceSamplerInstance.settings.oneTimeOnly)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Runing rerunable experiments is disabled");
        return false;
      }
      if (currentScienceValue < _AutomatedScienceSamplerInstance.settings.threshold)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Science value is less than cutoff threshold: ", currentScienceValue, "<", _AutomatedScienceSamplerInstance.settings.threshold);
        return false;
      }
      if (!currentExperiment.experiment.IsUnlocked())
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment is locked");
        return false;
      }
      return true;
    }

    public void DeployExperiment(ModuleScienceExperiment currentExperiment)
    {
      if (_AutomatedScienceSamplerInstance.settings.hideScienceDialog)
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

    public ScienceSubject GetScienceSubject(ModuleScienceExperiment experiment)
    {
      //experiment.BiomeIsRelevantWhile
      return ResearchAndDevelopment.GetExperimentSubject(experiment.experiment, ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel), FlightGlobals.currentMainBody, CurrentBiome(experiment.experiment));
    }

    public float GetScienceValue(ModuleScienceExperiment experiment, Dictionary<string, int> shipCotainsExperiments, ScienceSubject currentScienceSubject)
    {
      return Utilities.Science.GetScienceValue(shipCotainsExperiments, experiment.experiment, currentScienceSubject);
    }

    public List<Type> GetValidTypes()
    {
      var types = new List<Type>();
      types.Add(typeof(ModuleScienceExperiment));
      return types;
    }
    public bool CanReset(ModuleScienceExperiment experiment)
    {
      if (!experiment.Deployed)
      {
        _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Experiment isn't deployed!");
        return false;
      }
      if (experiment.GetData().Length > 0)
      {
        _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Experiment has data!");
        return false;
      }

      if (!experiment.resettable)
      {
        _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Experiment isn't resetable");
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
        _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Vessel has no scientist");
        return false;
      }
      _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Can reset");
      return true;
    }

    public void Reset(ModuleScienceExperiment experiment)
    {
      _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Reseting experiment");
      experiment.ResetExperiment();
    }

    private string CurrentBiome(ScienceExperiment experiment)
    {
      if (!experiment.BiomeIsRelevantWhile(ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel)))
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

    public bool CanTransfer(ModuleScienceExperiment experiment, ModuleScienceContainer moduleScienceContainer)
    {
      if (experiment.GetData().Length == 0)
      {
        _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Experiment has no data skiping transfer ", experiment.GetData().Length);
        return false;
      }
      if (!experiment.IsRerunnable())
      {
        if (!_AutomatedScienceSamplerInstance.settings.transferAllData)
        {
          _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Experiment isn't rerunnable and transferAllData is turned off.");
          return false;
        }
      }
      if (!_AutomatedScienceSamplerInstance.settings.dumpDuplicates)
      {
        foreach (var data in experiment.GetData())
        {
          if (moduleScienceContainer.HasData(data))
          {
            _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": Target already has experiment and dumping is disabled.");
            return false;
          }
        }
      }
      _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": We can transfer the science!");
      return true;
    }
    public void Transfer(ModuleScienceExperiment experiment, ModuleScienceContainer moduleScienceContainer)
    {
      _AutomatedScienceSamplerInstance.Log(experiment.experimentID, ": transfering");
      moduleScienceContainer.StoreData(new List<IScienceDataContainer>() { experiment }, _AutomatedScienceSamplerInstance.settings.dumpDuplicates);
    }
  }
}
