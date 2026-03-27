using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sever sever;
    [SerializeField] private TMP_Dropdown serverDropdown;
    [SerializeField] private TMP_Dropdown modeDropdown;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        if (sever == null)
        {
            sever = FindFirstObjectByType<Sever>();
        }
    }

    private void OnEnable()
    {
        if (sever != null)
        {
            sever.StateChanged += RefreshView;
        }

        BindControls();
        PopulateDropdowns();
        RefreshView();
    }

    private void OnDisable()
    {
        if (sever != null)
        {
            sever.StateChanged -= RefreshView;
        }

        UnbindControls();
    }

    private void BindControls()
    {
        if (serverDropdown != null)
        {
            serverDropdown.onValueChanged.AddListener(OnServerChanged);
        }

        if (modeDropdown != null)
        {
            modeDropdown.onValueChanged.AddListener(OnModeChanged);
        }

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveClicked);
        }
    }

    private void UnbindControls()
    {
        if (serverDropdown != null)
        {
            serverDropdown.onValueChanged.RemoveListener(OnServerChanged);
        }

        if (modeDropdown != null)
        {
            modeDropdown.onValueChanged.RemoveListener(OnModeChanged);
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(OnJoinClicked);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeaveClicked);
        }
    }

    private void PopulateDropdowns()
    {
        if (sever == null)
        {
            return;
        }

        if (serverDropdown != null)
        {
            PopulateDropdown(serverDropdown, sever.GetServerNames(), sever.SelectedServer);
        }

        if (modeDropdown != null)
        {
            PopulateDropdown(modeDropdown, sever.GetModeNames(), sever.SelectedMode);
        }
    }

    private void PopulateDropdown(TMP_Dropdown dropdown, IReadOnlyList<string> values, string currentValue)
    {
        dropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>(values.Count);
        int selectedIndex = 0;

        for (int i = 0; i < values.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(values[i]));
            if (values[i] == currentValue)
            {
                selectedIndex = i;
            }
        }

        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(selectedIndex);
    }

    private void OnServerChanged(int index)
    {
        if (sever == null)
        {
            return;
        }

        sever.SelectServerByIndex(index);
    }

    private void OnModeChanged(int index)
    {
        if (sever == null)
        {
            return;
        }

        sever.SelectModeByIndex(index);
    }

    private void OnJoinClicked()
    {
        if (sever == null)
        {
            return;
        }

        sever.JoinSelectedServer();
    }

    private void OnLeaveClicked()
    {
        if (sever == null)
        {
            return;
        }

        sever.LeaveServer();
    }

    private void RefreshView()
    {
        if (sever == null)
        {
            return;
        }

        if (statusText != null)
        {
            statusText.text = sever.GetStatusText();
        }

        bool connected = sever.IsConnected;
        bool joining = sever.IsJoining;

        if (serverDropdown != null)
        {
            serverDropdown.interactable = !connected && !joining;
        }

        if (modeDropdown != null)
        {
            modeDropdown.interactable = !connected && !joining;
        }

        if (joinButton != null)
        {
            joinButton.interactable = !connected && !joining;
        }

        if (leaveButton != null)
        {
            leaveButton.interactable = connected || joining;
        }
    }
}
