using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerboKatz.ASS
{
  public class Settings : SettingsBase<Settings>
  {
    public bool runAutoScience = false;
    public bool showSettings = false;
    public float threshold = 2;
    public bool oneTimeOnly = false;
    public float spriteFPS = 60;
    public bool resetExperiments;
    public bool hideScienceDialog;
    public bool transferAllData;
    public bool dumpDuplicates;
  }
}
