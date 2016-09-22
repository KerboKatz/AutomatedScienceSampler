using System;
using System.Collections.Generic;

namespace KerboKatz.ASS
{
  public interface IScienceActivator
  {
    AutomatedScienceSampler AutomatedScienceSampler { get; set; }

    //float GetScience();
    List<Type> GetValidTypes();

    ScienceSubject GetScienceSubject(ModuleScienceExperiment baseExperimentModule);

    float GetScienceValue(ModuleScienceExperiment baseExperimentModule, Dictionary<string, int> shipCotainsExperiments, ScienceSubject currentScienceSubject);

    bool CanRunExperiment(ModuleScienceExperiment baseExperimentModule, float currentScienceValue);

    void DeployExperiment(ModuleScienceExperiment baseExperimentModule);

    bool CanReset(ModuleScienceExperiment baseExperimentModule);

    void Reset(ModuleScienceExperiment baseExperimentModule);

    bool CanTransfer(ModuleScienceExperiment baseExperimentModule, ModuleScienceContainer moduleScienceContainer);

    void Transfer(ModuleScienceExperiment baseExperimentModule, ModuleScienceContainer moduleScienceContainer);
  }
}