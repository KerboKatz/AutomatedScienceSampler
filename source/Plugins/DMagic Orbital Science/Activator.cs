using DMagic.Part_Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KerboKatz;

namespace KerboKatz.ASS
{
  public class Activator : IScienceActivator
  {
    AutomatedScienceSampler _AutomatedScienceSamplerInstance;
    AutomatedScienceSampler IScienceActivator.AutomatedScienceSampler
    {
      get { return _AutomatedScienceSamplerInstance; }
      set { _AutomatedScienceSamplerInstance = value; }
    }

    public bool CanRunExperiment(ModuleScienceExperiment baseExperiment, float currentScienceValue)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
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
      return DMModuleScienceAnimate.conduct(baseExperiment);
    }

    public void DeployExperiment(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
      currentExperiment.gatherScienceData(_AutomatedScienceSamplerInstance.settings.hideScienceDialog);
    }

    public ScienceSubject GetScienceSubject(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
      return ResearchAndDevelopment.GetExperimentSubject(ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID), ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel), FlightGlobals.currentMainBody, CurrentBiome(currentExperiment));
    }

    public float GetScienceValue(ModuleScienceExperiment experiment, Dictionary<string, int> shipCotainsExperiments, ScienceSubject currentScienceSubject)
    {
      return Utilities.Science.GetScienceValue(shipCotainsExperiments, ResearchAndDevelopment.GetExperiment(experiment.experimentID), currentScienceSubject);
    }

    public List<Type> GetValidTypes()
    {
      var types = new List<Type>();
      types.Add(typeof(DMModuleScienceAnimate));

      Utilities.LoopTroughAssemblies((type) =>
      {
        if(type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(DMModuleScienceAnimate)))
        {
          types.Add(type);
        }
      });
      return types;
    }
    public bool CanReset(ModuleScienceExperiment experiment)
    {
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
      return true;
    }

    public void Reset(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
      _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Reseting experiment");
      currentExperiment.ResetExperiment();
    }

    private string CurrentBiome(DMModuleScienceAnimate baseModuleExperiment)
    {
      var experimentSituation = ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel);
      if (!baseModuleExperiment.experiment.BiomeIsRelevantWhile(experimentSituation))
        return string.Empty;

      //var currentModuleExperiment = baseModuleExperiment as DMModuleScienceAnimate;
      if ((baseModuleExperiment.bioMask & (int)experimentSituation) == 0)
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

    public bool CanTransfer(ModuleScienceExperiment baseExperiment, ModuleScienceContainer moduleScienceContainer)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
      if (!currentExperiment.IsRerunnable())
      {
        if (!_AutomatedScienceSamplerInstance.settings.transferAllData)
        {
          return false;
        }
      }
      if (!_AutomatedScienceSamplerInstance.settings.dumpDuplicates)
      {
        foreach (var data in currentExperiment.GetData())
        {
          if (moduleScienceContainer.HasData(data))
          {
            return false;
          }
        }
      }
      return true;
    }

    public void Transfer(ModuleScienceExperiment baseExperiment, ModuleScienceContainer moduleScienceContainer)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
      moduleScienceContainer.StoreData(new List<IScienceDataContainer>() { currentExperiment as DMModuleScienceAnimate }, _AutomatedScienceSamplerInstance.settings.dumpDuplicates);
    }
  }
}
