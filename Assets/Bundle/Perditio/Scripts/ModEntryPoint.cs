using System.Linq;
using HarmonyLib;
using UnityEngine;
using Modding;

namespace Perditio
{
    public class ModEntryPoint : IModEntryPoint
    {
		public const string Perditio_Map_Address = "Assets/Bundle/Perditio/Perditio.prefab";
		public const string Perditio_Map_Key = "tdAS9vY840iVGm-_fBrhnw";

        public void PreLoad()
        {
            Debug.Log("Perditio Preload");
        }

        public void PostLoad()
        {
            Debug.Log("Perditio PostLoad");
			Debug.Log("Perditio CONQUEST ONLY version: 1.0");

            Harmony harmony = new Harmony("nebulous.perditio");
            harmony.PatchAll();
        }

		public static readonly string[] SECTOR_NAMES_WORDLIST = {
    		"Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", 
    		"Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", 
    		"Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu"
		};

		public static readonly string[] SYSTEM_NAMES_WORDLIST = {
    		"Aether", "Arcanum", "Lumen", "Ignitus", "Cryosis", "Fulgur", "Aquos", "Virdis",
    		"Ignifer", "Umbra", "Luxor", "Pyrosis", "Glacium", "Magnus", "Terram", "Arcana",
    		"Volantis", "Obsidian", "Terrigen", "Pyrium", "Noctis", "Solaris", "Nebulus", "Sylvan",
    		"Gaius", "Celestis", "Fulmen", "Ferrum", "Aquaorbis", "Lunaris", "Ignem", "Arboris",
    		"Petrus", "Nimbus", "Pyroclasm", "Aquis", "Terrafer", "Magica", "Stellas", "Umbraxis",
    		"Solum", "Chronos", "Aerius", "Lignum", "Tenebris", "Caelum", "Gelidus", "Sideris",
    		"Tempus", "Arboreus", "Aurorum", "Fulgoris", "Igniscor", "Lapis", "Herbus", "Marinus",
    		"Aerifer", "Nocturnus", "Ventus", "Pyrophor", "Vitrium", "Fluctus", "Ignavus", "Elementum",
    		"Flamma", "Umbros", "Ardens", "Gelumor", "Aquisium", "Terramorf", "Sanguis", "Lunifer",
    		"Ignipotent", "Terraquor", "Lucidus", "Obscurus", "Frigus", "Gelicor", "Fulget", "Vires",
    		"Volantisor", "Praecantor", "Arcantus", "Venenifer", "Pyrofer", "Aquaflux", "Ignacis", "Lunarisor",
    		"Ferraris", "Petrifer", "Tempestus", "Arborflex", "Luxorum", "Caelifer", "Terrapotent", "Aetheris",
    		"Igniferor", "Umbraquor", "Siderium", "Ventorus", "Glacior", "Aerium", "Volanter", "Celestium",
    		"Maritum", "Pyrolux", "Fulgoror", "Noctisium", "Stellasium", "Gelifer", "Tempusor", "Obsidior",
    		"Herboris", "Pyroclasmor", "Umbrafer", "Fluctusor", "Lucior", "Magnusor", "Arcantum", "Arborisium",
    		"Gelidum", "Petrusor", "Siderius", "Pyroxis", "Ignavium", "Aquisor", "Umbraform", "Terrum",
    		"Lignis", "Fulgorfer", "Aetherflux", "Chronor", "Igniflux", "Caelisium", "Ventor", "Gelicium",
    		"Flammaor", "Ignitusor", "Terriform", "Aeriferor", "Tempestor", "Sylvanius", "Caelumor", "Frigusor",
    		"Lucidusor", "Sanguor", "Fulmenor", "Obscurium", "Luxfer", "Elementor", "Ferrarisor", "Vitrior",
    		"Praecantoror", "Viresor", "Terranix", "Pyroclasmium", "Gelidumor", "Ignipotenor", "Pyroclastor", "Obsidianor",
    		"Umbraxisor", "Virdisor", "Nocturium", "Venenium", "Ignifex", "Tempusium", "Arborflexor", "Sylvanor",
    		"Gaiusor", "Ferrumor", "Arcanaor", "Elementium", "Pyrosium", "Fluctusium", "Vitrius", "Umbrium",
    		"Nocturnor", "Aetherium", "Petrusium", "Ignavor", "Ignitor", "Luxorium", "Ventusium", "Chronosium",
    		"Marinius", "Aetherfluxor", "Umbraferor", "Glaciumor", "Lunarisium", "Obscurusor", "Terrigenor", "Lignumor",
    		"Arborium", "Tempestium", "Celestior", "Gelidusor", "Fulgorisor", "Siderisium", "Ignisium", "Petriferor",
    		"Arborflexium", "Frigusium", "Herbusor", "Elementoror", "Magnusium", "Igniferium", "Sideriumor", "Caelium",
    		"Pyroferor", "Fulmenoror", "Caeliumor", "Terranixor", "Pyriumor", "Aquisiumor", "Lignisium", "Pyrosisor",
    		"Umbraferium", "Elementumor", "Aquor", "Ignaxium", "Umbrius", "Ferrarisium", "Obsidianoror", "Vitriumor",
    		"Noctisiumor", "Tempestusor", "Arborisiumor", "Terramor", "Aetherfer", "Igniferoror", "Luxius", "Gaiusium",
    		"Chronosor", "Gelumoror", "Tempusoror", "Aquosium", "Ignacium", "Lucidusium", "Terrigenium", "Flammaoror",
    		"Viresium", "Obscuriumor", "Tempestiumor", "Ferrium", "Petrusiumor", "Gelium", "Ignisiumor", "Caeliferium",
    		"Fluctusiumor", "Aetherius", "Pyrosiumor", "Sylvanium", "Luminor", "Obsidium", "Ignitoror", "Arcanor",
    		"Pyroclastium", "Ventoror", "Umbraxi", "Chronium", "Petrusiumoror", "Lignus", "Viresoror", "Umbrosium",
    		"Gelumium", "Pyroclasmiumor", "Nocturnium", "Fulmenium", "Pyrosiumoror", "Glacius", "Elementiumor", "Fulgororor",
    		"Sanguisium", "Luminoror", "Ignacius", "Geliciumor", "Terramium", "Pyroclastoror", "Vitriusor", "Obscurior",
    		"Petrusiumium", "Venenumor", "Fulgetor", "Caeliferoror", "Terrumor", "Luminororor", "Vitriumoror", "Tempusiumium",
    		"Gaiusoror", "Sylvaniusor", "Frigusiumor", "Arborisior", "Aetheriumor", "Chronosoror", "Ignaxiumor", "Tempestorium",
    		"Ferrarisiumor", "Virdisium", "Umbraxisoror", "Aetherferor", "Lucidusiumor", "Gelidumoror", "Umbriumor",
    		"Pyrosisium", "Terrigeniumor", "Fulmenius", "Ventusoror", "Luminior", "Flammaororor", "Nocturniumor", "Pyriumium",
    		"Terranixium", "Arcanumor"
		};
    }
}
