using UnityEcho.Objectives;
using UnityEcho.UI;
using UnityEngine;

namespace UnityEcho.Demo
{
    public class ObjectiveNotificationController : MonoBehaviour
    {
        [SerializeField]
        private GameObject _notificationElement;

        [SerializeField]
        private AudioSource _notificationSound;

        private ObjectivesManager _objectivesManager;

        private Tablet _tablet;

        private void Start()
        {
            _notificationElement.SetActive(_objectivesManager.StartedObjectives.Count > 0);
            _objectivesManager = FindObjectOfType<ObjectivesManager>();
            _tablet = FindObjectOfType<Tablet>();
            _tablet.OnPanelActivate += TabletOnOnPanelActivate;
            _objectivesManager.OnObjectiveStarted.AddListener(OnObjectiveStarted);
        }

        private void OnDestroy()
        {
            _tablet.OnPanelActivate -= TabletOnOnPanelActivate;
            _objectivesManager.OnObjectiveStarted.RemoveListener(OnObjectiveStarted);
        }

        private void TabletOnOnPanelActivate(TabletPanel obj)
        {
            DismissNotification();
        }

        private void OnObjectiveStarted()
        {
            _notificationElement.SetActive(true);
            _notificationSound.Play();
        }

        public void DismissNotification()
        {
            _notificationElement.SetActive(false);
        }
    }
}