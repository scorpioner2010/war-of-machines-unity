using System.Collections.Generic;
using Game.Scripts.Testing;
using UnityEditor;

namespace Toras.Editor
{
    [CustomEditor(typeof(VehicleTestRuntimeSettings))]
    public class VehicleTestRuntimeSettingsEditor : UnityEditor.Editor
    {
        private static readonly IReadOnlyDictionary<string, string> Documentation =
            new Dictionary<string, string>
            {
                ["activateTestParameters"] =
                    "Вмикає тестову заміну характеристик перезаряджання та боєзапасу лише у VehicleTest. Наприклад: увімкніть, щоб перевіряти стрільбу з reloadTime = 0.25 і shellsCount = 999 без зміни даних машини в API.",
                ["reloadTime"] =
                    "Тестова тривалість перезаряджання в секундах, яка застосовується коли activateTestParameters увімкнено. Наприклад: 0.25 означає один готовий постріл приблизно кожні чверть секунди.",
                ["shellsCount"] =
                    "Тестова кількість снарядів після спавну машини, яка застосовується коли activateTestParameters увімкнено. Наприклад: 999 зручно для тривалої перевірки стрільби без постійного повторного спавну.",
                ["forceFullyAimedAccuracyOnly"] =
                    "Примусово тримає гармату повністю зведеною у VehicleTest, але точність машини все ще визначає розмір кола та розкид пострілу. Наприклад: увімкніть, щоб порівняти точність двох машин без очікування зведення після руху.",
                ["createHitMarkerSphere"] =
                    "Створює тестову сферу в точці авторитетного влучання снаряда. Наприклад: увімкніть, щоб візуально перевірити, куди сервер зарахував удар по броні або землі.",
                ["hitMarkerRadius"] =
                    "Радіус тестової сфери влучання у світових метрах. Наприклад: 0.18 створює сферу діаметром 0.36 метра.",
                ["hitMarkerColor"] =
                    "Колір тестової сфери влучання. Наприклад: яскраво-жовтий колір добре видно на темній землі та корпусі машини."
            };

        public override void OnInspectorGUI()
        {
            DocumentedSettingsInspector.Draw(
                serializedObject,
                "Тестові параметри діють лише у сцені VehicleTest. Наведіть курсор на назву параметра, щоб побачити український опис і приклад використання.",
                Documentation);
        }
    }
}
