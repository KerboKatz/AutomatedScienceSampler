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
      return DMModuleScienceAnimate.conduct(baseExperiment);
    }

    public void DeployExperiment(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
      currentExperiment.gatherScienceData(_AutomatedScienceSamplerInstance.settings.hideScienceDialog);
    }

    public ScienceSubject GetScienceSubject(ScienceExperiment experiment)
    {
      //experiment.BiomeIsRelevantWhile
      return ResearchAndDevelopment.GetExperimentSubject(experiment, ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel), FlightGlobals.currentMainBody, CurrentBiome(experiment));
    }

    public float GetScienceValue(ScienceExperiment experiment, Dictionary<string, int> shipCotainsExperiments, ScienceSubject currentScienceSubject)
    {
      return Utilities.Science.getScienceValue(shipCotainsExperiments, experiment, currentScienceSubject);
    }

    public List<Type> GetValidTypes()
    {
      var allTypes = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                   from type in assembly.GetTypes()
                   where type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(DMModuleScienceAnimate))
                   select type;
      var types = new List<Type>(allTypes);
      types.Add(typeof(DMModuleScienceAnimate));
      foreach(var t in types)
      {
        Debug.Log(t);
      }
      return types;
    }
    public bool CanReset(ModuleScienceExperiment experiment)
    {
      if (!experiment.resettable)
      {
        _AutomatedScienceSamplerInstance.Log(experiment.experimentID , ": Experiment isn't resetable");
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
        _AutomatedScienceSamplerInstance.Log(experiment.experimentID , ": Vessel has no scientist");
        return false;
      }
      return true;
    }

    public void Reset(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimate;
      _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID , ": Reseting experiment");
      currentExperiment.ResetExperiment();
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
      if (!experiment.IsRerunnable())
      {
        if (!_AutomatedScienceSamplerInstance.settings.transferAllData)
        {
          return false;
        }
      }
      if (!_AutomatedScienceSamplerInstance.settings.dumpDuplicates)
      {
        var scienceData = moduleScienceContainer.GetData();
        foreach (var data in experiment.GetData())
        {
          if (scienceData.Contains(data))
          {
            return false;
          }
        }
      }
      return true;
    }

    public void Transfer(ModuleScienceExperiment experiment, ModuleScienceContainer moduleScienceContainer)
    {
      moduleScienceContainer.StoreData(new List<IScienceDataContainer>() { experiment as DMModuleScienceAnimate }, _AutomatedScienceSamplerInstance.settings.dumpDuplicates);
    }
  }
}
