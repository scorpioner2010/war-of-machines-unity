using System;
using System.Globalization;
using System.Text;
using DG.Tweening;
using Game.Scripts.API;
using Game.Scripts.Gameplay.Robots;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Game.Scripts.UI.MainMenu
{
    public class VehicleSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button slotButton;
        [SerializeField] private Transform vehicleRoot;
        [SerializeField] private RectTransform specifications;
        [SerializeField] private CanvasGroup specificationsCanvasGroup;
        [SerializeField] private TMP_Text specificationsText;
        [SerializeField] private float specificationsFadeDuration = 0.18f;
        [SerializeField] private float specificationsMoveOffset = 12f;

        private VehicleItemView _vehicle;
        private int _vehicleId;
        private VehicleSlotVehicleData _vehicleData;
        private Action<int> _onVehicleSelected;
        private Tween _specificationsTween;
        private Vector2 _specificationsShownPosition;
        private Vector2 _specificationsHiddenPosition;
        private bool _isInitialized;

        private static GameObject _specificationsTemplate;

        public int SlotIndex { get; private set; }
        public bool HasVehicle => _vehicle != null;

        private void Awake()
        {
            CacheSpecifications();
            HideSpecificationsImmediate();
        }

        private void OnDisable()
        {
            HideSpecificationsImmediate();
        }

        private void OnDestroy()
        {
            if (_specificationsTween != null)
            {
                _specificationsTween.Kill();
                _specificationsTween = null;
            }
        }

        public void InitEmpty(int slotIndex)
        {
            CacheSpecifications();

            SlotIndex = slotIndex;
            _vehicleId = 0;
            _vehicleData = null;
            _onVehicleSelected = null;

            if (_vehicle != null)
            {
                Destroy(_vehicle.gameObject);
                _vehicle = null;
            }

            ApplySpecifications(null);
            HideSpecificationsImmediate();

            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(OnSlotClicked);
                slotButton.interactable = false;
            }
        }

        public void PlaceVehicle(VehicleItemView vehicle, VehicleSlotVehicleData data, Action<int> onVehicleSelected)
        {
            if (vehicle == null || data == null)
            {
                return;
            }

            if (_vehicle != null)
            {
                Destroy(_vehicle.gameObject);
            }

            _vehicle = vehicle;
            _vehicleId = data.VehicleId;
            _vehicleData = data;
            _onVehicleSelected = onVehicleSelected;

            Transform root = vehicleRoot != null ? vehicleRoot : transform;
            _vehicle.transform.SetParent(root, false);
            _vehicle.Apply(data);
            ApplySpecifications(data);

            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(OnSlotClicked);
                slotButton.onClick.AddListener(OnSlotClicked);
                slotButton.interactable = !data.IsSelected;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_vehicleData == null || !HasVehicle)
            {
                return;
            }

            ShowSpecifications();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideSpecifications();
        }

        private void OnSlotClicked()
        {
            if (_vehicleId <= 0 || _onVehicleSelected == null)
            {
                return;
            }

            _onVehicleSelected.Invoke(_vehicleId);
        }

        private void CacheSpecifications()
        {
            if (_isInitialized)
            {
                return;
            }

            if (specifications == null)
            {
                Transform found = FindChildRecursive(transform, "Specifications");
                if (found != null)
                {
                    specifications = found as RectTransform;
                }
            }

            if (specifications == null)
            {
                CloneSpecificationsTemplate();
            }

            if (specifications == null)
            {
                CreateSpecificationsFallback();
            }

            if (specificationsCanvasGroup == null)
            {
                specificationsCanvasGroup = specifications.GetComponent<CanvasGroup>();
                if (specificationsCanvasGroup == null)
                {
                    specificationsCanvasGroup = specifications.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (specificationsText == null)
            {
                specificationsText = specifications.GetComponentInChildren<TMP_Text>(true);
            }

            specificationsCanvasGroup.interactable = false;
            specificationsCanvasGroup.blocksRaycasts = false;

            _specificationsShownPosition = specifications.anchoredPosition;
            _specificationsHiddenPosition = _specificationsShownPosition - new Vector2(0f, specificationsMoveOffset);
            _isInitialized = true;
        }

        private void CloneSpecificationsTemplate()
        {
            if (_specificationsTemplate == null)
            {
                RectTransform[] templates = Resources.FindObjectsOfTypeAll<RectTransform>();
                for (int i = 0; i < templates.Length; i++)
                {
                    RectTransform template = templates[i];
                    if (template == null
                        || template.name != "Specifications"
                        || template.IsChildOf(transform)
                        || !template.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    _specificationsTemplate = template.gameObject;
                    break;
                }
            }

            if (_specificationsTemplate == null)
            {
                return;
            }

            GameObject instance = Instantiate(_specificationsTemplate, transform, false);
            instance.name = "Specifications";
            specifications = instance.GetComponent<RectTransform>();
            specificationsCanvasGroup = instance.GetComponent<CanvasGroup>();
            specificationsText = instance.GetComponentInChildren<TMP_Text>(true);
        }

        private void CreateSpecificationsFallback()
        {
            GameObject specificationsObject = new GameObject("Specifications", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            specificationsObject.transform.SetParent(transform, false);

            specifications = specificationsObject.GetComponent<RectTransform>();
            specifications.anchorMin = new Vector2(0.5f, 0.5f);
            specifications.anchorMax = new Vector2(0.5f, 0.5f);
            specifications.pivot = new Vector2(0.5f, 0.5f);
            specifications.anchoredPosition = new Vector2(0f, 295f);
            specifications.sizeDelta = new Vector2(220f, 345f);

            Image background = specificationsObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.12f, 0.94f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(specifications, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 12f);
            textRect.offsetMax = new Vector2(-14f, -12f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.fontSize = 18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 9f;
            text.fontSizeMax = 18f;
            text.color = Color.white;
            text.lineSpacing = -8f;

            specificationsCanvasGroup = specificationsObject.GetComponent<CanvasGroup>();
            specificationsText = text;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindChildRecursive(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void ApplySpecifications(VehicleSlotVehicleData data)
        {
            if (specificationsText == null)
            {
                return;
            }

            if (data == null)
            {
                specificationsText.text = string.Empty;
                return;
            }

            StringBuilder builder = new StringBuilder(512);
            builder.Append("<b>").Append(string.IsNullOrEmpty(data.Name) ? "Vehicle" : data.Name).Append("</b>");

            if (data.Level > 0 || !string.IsNullOrEmpty(data.Class))
            {
                builder.AppendLine();
                builder.Append("Tier ");
                builder.Append(ToRoman(data.Level));

                if (!string.IsNullOrEmpty(data.Class))
                {
                    builder.Append("  ").Append(data.Class);
                }
            }

            bool hasFirepower = data.DamageMin > 0f
                || data.DamageMax > 0f
                || data.Penetration > 0
                || data.ShellSpeed > 0f
                || data.ShellsCount > 0
                || data.ReloadTime > 0f
                || data.Accuracy > 0f
                || data.AimTime > 0f;
            if (hasFirepower)
            {
                AppendHeader(builder, "FIREPOWER");
                AppendRange(builder, "Damage", data.DamageMin, data.DamageMax, string.Empty, "0.#");
                AppendInt(builder, "Penetration", data.Penetration, " mm");
                AppendFloat(builder, "Shell speed", data.ShellSpeed, " m/s", "0.#");
                AppendInt(builder, "Ammo", data.ShellsCount, string.Empty);
                AppendFloat(builder, "Reload", data.ReloadTime, " s", "0.##");
                AppendFloat(builder, "Accuracy", data.Accuracy, string.Empty, "0.##");
                AppendFloat(builder, "Aim time", data.AimTime, " s", "0.##");
            }

            bool hasSurvivability = data.Hp > 0
                || !string.IsNullOrWhiteSpace(data.HullArmor)
                || !string.IsNullOrWhiteSpace(data.TurretArmor);
            if (hasSurvivability)
            {
                AppendHeader(builder, "SURVIVABILITY");
                AppendInt(builder, "Hit points", data.Hp, string.Empty);
                AppendText(builder, "Hull armor", data.HullArmor);
                AppendText(builder, "Turret armor", data.TurretArmor);
            }

            bool hasMobility = data.Speed > 0f
                || data.Acceleration > 0f
                || data.TraverseSpeed > 0f
                || data.TurretTraverseSpeed > 0f;
            if (hasMobility)
            {
                AppendHeader(builder, "MOBILITY");
                AppendFloat(builder, "Top speed", data.Speed, " km/h", "0.#");
                AppendFloat(builder, "Acceleration", data.Acceleration, string.Empty, "0.#");
                AppendFloat(builder, "Hull traverse", data.TraverseSpeed, " deg/s", "0.#");
                AppendFloat(builder, "Turret traverse", data.TurretTraverseSpeed, " deg/s", "0.#");
            }

            specificationsText.text = builder.ToString();
        }

        private static void AppendHeader(StringBuilder builder, string label)
        {
            builder.AppendLine();
            builder.Append("<color=#D6B45D><b>").Append(label).Append("</b></color>");
        }

        private static void AppendInt(StringBuilder builder, string label, int value, string suffix)
        {
            if (value <= 0)
            {
                return;
            }

            builder.AppendLine();
            builder.Append(label).Append(": ").Append(value).Append(suffix);
        }

        private static void AppendFloat(StringBuilder builder, string label, float value, string suffix, string format)
        {
            if (value <= 0f)
            {
                return;
            }

            builder.AppendLine();
            builder.Append(label).Append(": ").Append(value.ToString(format, CultureInfo.InvariantCulture)).Append(suffix);
        }

        private static void AppendRange(StringBuilder builder, string label, float min, float max, string suffix, string format)
        {
            if (min <= 0f && max <= 0f)
            {
                return;
            }

            builder.AppendLine();
            builder.Append(label).Append(": ");
            if (Mathf.Approximately(min, max))
            {
                builder.Append(min.ToString(format, CultureInfo.InvariantCulture)).Append(suffix);
                return;
            }

            builder.Append(min.ToString(format, CultureInfo.InvariantCulture))
                .Append("-")
                .Append(max.ToString(format, CultureInfo.InvariantCulture))
                .Append(suffix);
        }

        private static void AppendText(StringBuilder builder, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            builder.AppendLine();
            builder.Append(label).Append(": ").Append(value).Append(" mm");
        }

        private static string ToRoman(int value)
        {
            if (value <= 0)
            {
                return "-";
            }

            if (value == 1)
            {
                return "I";
            }

            if (value == 2)
            {
                return "II";
            }

            if (value == 3)
            {
                return "III";
            }

            if (value == 4)
            {
                return "IV";
            }

            if (value == 5)
            {
                return "V";
            }

            if (value == 6)
            {
                return "VI";
            }

            if (value == 7)
            {
                return "VII";
            }

            if (value == 8)
            {
                return "VIII";
            }

            if (value == 9)
            {
                return "IX";
            }

            if (value == 10)
            {
                return "X";
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        private void ShowSpecifications()
        {
            if (specifications == null || specificationsCanvasGroup == null)
            {
                return;
            }

            if (_specificationsTween != null)
            {
                _specificationsTween.Kill();
            }

            specifications.gameObject.SetActive(true);
            specifications.SetAsLastSibling();
            specifications.anchoredPosition = _specificationsHiddenPosition;
            specificationsCanvasGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(specificationsCanvasGroup.DOFade(1f, specificationsFadeDuration));
            sequence.Join(specifications.DOAnchorPos(_specificationsShownPosition, specificationsFadeDuration).SetEase(Ease.OutCubic));
            _specificationsTween = sequence;
        }

        private void HideSpecifications()
        {
            if (specifications == null || specificationsCanvasGroup == null)
            {
                return;
            }

            if (_specificationsTween != null)
            {
                _specificationsTween.Kill();
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Join(specificationsCanvasGroup.DOFade(0f, specificationsFadeDuration));
            sequence.Join(specifications.DOAnchorPos(_specificationsHiddenPosition, specificationsFadeDuration).SetEase(Ease.OutCubic));
            sequence.OnComplete(() =>
            {
                if (specifications != null)
                {
                    specifications.gameObject.SetActive(false);
                }
            });
            _specificationsTween = sequence;
        }

        private void HideSpecificationsImmediate()
        {
            CacheSpecifications();

            if (_specificationsTween != null)
            {
                _specificationsTween.Kill();
                _specificationsTween = null;
            }

            if (specifications == null || specificationsCanvasGroup == null)
            {
                return;
            }

            specificationsCanvasGroup.alpha = 0f;
            specifications.anchoredPosition = _specificationsHiddenPosition;
            specifications.gameObject.SetActive(false);
        }
    }

    public class VehicleSlotVehicleData
    {
        public int VehicleId;
        public string Name;
        public Sprite Icon;
        public bool IsSelected;
        public string Class;
        public int Level;
        public int Hp;
        public int Penetration;
        public float ShellSpeed;
        public int ShellsCount;
        public float DamageMin;
        public float DamageMax;
        public float ReloadTime;
        public float Accuracy;
        public float AimTime;
        public float Speed;
        public float Acceleration;
        public float TraverseSpeed;
        public float TurretTraverseSpeed;
        public string TurretArmor;
        public string HullArmor;

        public void ApplyVehicleLite(VehicleLite vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(Name))
            {
                Name = vehicle.name;
            }

            Class = vehicle.@class;
            Level = vehicle.level;
            Hp = vehicle.hp;
            Penetration = vehicle.penetration;
            ShellSpeed = VehicleRuntimeStats.ResolveShellSpeed(vehicle.shellSpeed);
            ShellsCount = VehicleRuntimeStats.ResolveShellsCount(vehicle.shellsCount);
            VehicleRuntimeStats.ResolveDamageRange(vehicle.damageMin, vehicle.damageMax, out DamageMin, out DamageMax);
            ReloadTime = vehicle.reloadTime;
            Accuracy = vehicle.accuracy;
            AimTime = vehicle.aimTime;
            Speed = vehicle.speed;
            Acceleration = vehicle.acceleration;
            TraverseSpeed = vehicle.traverseSpeed;
            TurretTraverseSpeed = vehicle.turretTraverseSpeed;
            TurretArmor = vehicle.turretArmor;
            HullArmor = vehicle.hullArmor;
        }
    }
}
