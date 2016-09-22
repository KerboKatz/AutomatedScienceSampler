using DMModuleScienceAnimateGeneric;
using System;
using System.Collections.Generic;

namespace KerboKatz.ASS.DMMSAG
{
  public class Activator : IScienceActivator
  {
    private AutomatedScienceSampler _AutomatedScienceSamplerInstance;

    AutomatedScienceSampler IScienceActivator.AutomatedScienceSampler
    {
      get { return _AutomatedScienceSamplerInstance; }
      set { _AutomatedScienceSamplerInstance = value; }
    }

    public bool CanRunExperiment(ModuleScienceExperiment baseExperiment, float currentScienceValue)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      if (currentScienceValue < _AutomatedScienceSamplerInstance.craftSettings.threshold)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Science value is less than cutoff threshold: ", currentScienceValue, "<", _AutomatedScienceSamplerInstance.craftSettings.threshold);
        return false;
      }
      if (!currentExperiment.rerunnable && !_AutomatedScienceSamplerInstance.craftSettings.oneTimeOnly)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Runing rerunable experiments is disabled");
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
      if (!currentExperiment.experiment.IsUnlocked())
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment is locked");
        return false;
      }
      return currentExperiment.canConduct();
    }

    public void DeployExperiment(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      currentExperiment.gatherScienceData(_AutomatedScienceSamplerInstance.craftSettings.hideScienceDialog);
    }

    public ScienceSubject GetScienceSubject(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      if (DMSciAnimAPI.isAsteroidGrappled(currentExperiment))
      {
        return DMSciAnimAPI.getAsteroidSubject(currentExperiment);
      }
      else
      {
        ExperimentSituations situation = ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel);
        var biome = currentExperiment.getBiome(situation);
        _AutomatedScienceSamplerInstance.Log(biome, "_", situation, "_", ResearchAndDevelopment.GetExperimentSubject(ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID), situation, FlightGlobals.currentMainBody, biome) == null);
        return ResearchAndDevelopment.GetExperimentSubject(ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID), situation, FlightGlobals.currentMainBody, biome);
      }
    }

    public float GetScienceValue(ModuleScienceExperiment baseExperiment, Dictionary<string, int> shipCotainsExperiments, ScienceSubject currentScienceSubject)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      var scienceExperiment = ResearchAndDevelopment.GetExperiment(baseExperiment.experimentID);
      return Utilities.Science.GetScienceValue(shipCotainsExperiments, scienceExperiment, currentScienceSubject);
    }

    public bool CanReset(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      if (!currentExperiment.Inoperable)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment isn't inoperable");
        return false;
      }
      if (!currentExperiment.Deployed)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment isn't deployed!");
        return false;
      }
      if ((currentExperiment as IScienceDataContainer).GetScienceCount() > 0)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment has data!");
        return false;
      }
      if (!currentExperiment.resettable)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment isn't resetable");
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
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Vessel has no scientist");
        return false;
      }
      return true;
    }

    public void Reset(ModuleScienceExperiment baseExperiment)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Reseting experiment");
      currentExperiment.ResetExperiment();
    }

    public bool CanTransfer(ModuleScienceExperiment baseExperiment, ModuleScienceContainer moduleScienceContainer)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      if ((currentExperiment as IScienceDataContainer).GetScienceCount() == 0)
      {
        _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment has no data skiping transfer. Data found: ", (currentExperiment as IScienceDataContainer).GetScienceCount());
        return false;
      }
      if (!currentExperiment.IsRerunnable())
      {
        if (!_AutomatedScienceSamplerInstance.craftSettings.transferAllData)
        {
          _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Experiment isn't rerunnable and transferAllData is turned off.");
          return false;
        }
      }
      if (!_AutomatedScienceSamplerInstance.craftSettings.dumpDuplicates)
      {
        foreach (var data in (currentExperiment as IScienceDataContainer).GetData())
        {
          if (moduleScienceContainer.HasData(data))
          {
            _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": Target already has experiment and dumping is disabled.");
            return false;
          }
        }
      }
      _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": We can transfer the science!");
      return true;
    }

    public void Transfer(ModuleScienceExperiment baseExperiment, ModuleScienceContainer moduleScienceContainer)
    {
      var currentExperiment = baseExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric;
      _AutomatedScienceSamplerInstance.Log(currentExperiment.experimentID, ": transfering");
      moduleScienceContainer.StoreData(new List<IScienceDataContainer>() { currentExperiment as DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric }, _AutomatedScienceSamplerInstance.craftSettings.dumpDuplicates);
    }

    public List<Type> GetValidTypes()
    {
      var types = new List<Type>();
      types.Add(typeof(DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric));

      Utilities.LoopTroughAssemblies((type) =>
      {
        if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(DMModuleScienceAnimateGeneric.DMModuleScienceAnimateGeneric)))
        {
          types.Add(type);
        }
      });
      return types;
    }
  }
}