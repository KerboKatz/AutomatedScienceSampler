using System;
using System.Collections.Generic;

namespace KerboKatz.ASS
{
  public interface IScienceActivator
  {
    AutomatedScienceSampler AutomatedScienceSampler { get; set; }

    //float GetScience();
    List<Type> GetValidTypes();
    ScienceSubject GetScienceSubject(ScienceExperiment experiment);
    float GetScienceValue(ScienceExperiment experiment, Dictionary<string, int> shipCotainsExperiments, ScienceSubject currentScienceSubject);
    bool CanRunExperiment(ModuleScienceExperiment currentExperiment, float currentScienceValue);
    void DeployExperiment(ModuleScienceExperiment currentExperiment);
    bool CanReset(ModuleScienceExperiment experiment);
    void Reset(ModuleScienceExperiment experiment);
    bool CanTransfer(ModuleScienceExperiment experiment, ModuleScienceContainer moduleScienceContainer);
    void Transfer(ModuleScienceExperiment experiment, ModuleScienceContainer moduleScienceContainer);
  }
}