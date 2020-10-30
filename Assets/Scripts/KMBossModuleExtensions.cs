using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KMBossModuleExtensions : KMBossModule {

    public string[] GetAttachedIgnoredModuleIDs(KMBombModule modSelf, string[] @default = null)
    {
        return GetAttachedIgnoredModuleIDs(modSelf.ModuleDisplayName, @default);
    }

    public string[] GetAttachedIgnoredModuleIDs(string moduleName, string[] @default = null)
    {
        string[] modNamesIgnored = GetIgnoredModules(moduleName, @default);
        // Redirect to KM Boss Module for standard boss module handling. 
        
        if (Application.isEditor)
        {
            return @default ?? new string[0];
        }
        

        KMBomb bombAttached = gameObject.GetComponentInParent<KMBomb>();
        if (bombAttached == null)
        {
            Debug.LogFormat("[KMBossModuleExtensions] Unable to grab ignored mod IDs for “{0}” because KMBomb does not exist.", moduleName);
            return @default ?? new string[0];
        }
        KMBombModule[] allSolvables = bombAttached.gameObject.GetComponentsInChildren<KMBombModule>();
        if (allSolvables == null || !allSolvables.Any())
        {
            Debug.LogFormat("[KMBossModuleExtensions] Unable to grab ignored mod IDs for “{0}” because there are no solvable modules.", moduleName);
            return @default ?? new string[0];
        }
        string[] output = allSolvables.Where(a => modNamesIgnored.Contains(a.ModuleDisplayName)).Select(a => a.ModuleType).Distinct().ToArray();
        Debug.LogFormat("[KMBossModuleExtensions] Successfully grabbed ALL ignored module ids from the given bomb for “{0}”. Returning this: {1}", moduleName, output == null || !output.Any() ? "<null>" : output.Join(", "));
        return output;
    }

}
