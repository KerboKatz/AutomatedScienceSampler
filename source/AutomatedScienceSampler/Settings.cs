using KerboKatz.UI;
using System.Collections.Generic;

namespace KerboKatz.ASS
{
  public class Settings : SettingsBase<Settings>
  {
    public bool showSettings = false;
    public float spriteFPS = 60;
    private Dictionary<string, PerCraftSetting> _craftSettings = new Dictionary<string, PerCraftSetting>();
    public List<PerCraftSetting> craftSettings = new List<PerCraftSetting>();
    public bool perCraftSetting;
    public string lastGUID;
    public double refreshTime = 1;
    public bool interruptTimeWarp = true;
    public bool dropOutOfWarp = false;

    protected override void OnLoaded()
    {
      _craftSettings.Clear();
      foreach (var setting in craftSettings)
      {
        _craftSettings.Add(setting.guid, setting);
      }
    }

    protected override void OnSave()
    {
      craftSettings = new List<PerCraftSetting>(_craftSettings.Values);
    }

    public PerCraftSetting GetSettingsForCraft(string guid)
    {
      PerCraftSetting setting;
      if (!_craftSettings.TryGetValue(guid, out setting))
      {
        if (lastGUID.IsNullOrWhiteSpace())
        {
          setting = new PerCraftSetting();
        }
        else
        {
          setting = GetSettingsForCraft(lastGUID).Clone();
        }
        setting.guid = guid;
        _craftSettings.Add(guid, setting);
      }
      lastGUID = guid;
      return setting;
    }
  }

  public class PerCraftSetting
  {
    public string guid;
    public float threshold = 2;
    public bool runAutoScience = false;
    public bool oneTimeOnly = false;
    public bool resetExperiments;
    public bool hideScienceDialog;
    public bool transferAllData;
    public bool dumpDuplicates;
    public bool doEVAOnlyIfGroundedWhenLanded;
    public int currentContainer;

    public PerCraftSetting Clone()
    {
      var setting = new PerCraftSetting();
      setting.threshold = threshold;
      setting.runAutoScience = runAutoScience;
      setting.oneTimeOnly = oneTimeOnly;
      setting.resetExperiments = resetExperiments;
      setting.hideScienceDialog = hideScienceDialog;
      setting.transferAllData = transferAllData;
      setting.dumpDuplicates = dumpDuplicates;
      setting.doEVAOnlyIfGroundedWhenLanded = doEVAOnlyIfGroundedWhenLanded;
      return setting;
    }
  }
}