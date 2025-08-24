using System.Collections;
using UnityEngine;

namespace UnityEcho.UI
{
    public class TabletPanel : MonoBehaviour
    {
        [SerializeField]
        [Multiline]
        private string _label;

        [SerializeField]
        private GameObject _wrapper;

        private CanvasGroup _canvasGroup;

        private float _fadeVel;

        public bool Active => _wrapper.activeSelf;

        public string Label => _label;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private IEnumerator Fade(float to)
        {
            _wrapper.SetActive(true);

            while (!Mathf.Approximately(_canvasGroup.alpha, to))
            {
                _canvasGroup.alpha = Mathf.SmoothDamp(_canvasGroup.alpha, to, ref _fadeVel, Time.deltaTime);
                yield return null;
            }

            _wrapper.SetActive(!Mathf.Approximately(_canvasGroup.alpha, 0));
        }

        public void SetActive(bool active)
        {
            if (active == Active)
            {
                return;
            }

            StopAllCoroutines();
            if (_canvasGroup && gameObject.activeInHierarchy)
            {
                if (active)
                {
                    StartCoroutine(Fade(1));
                }
                else
                {
                    StartCoroutine(Fade(0));
                }
            }
            else
            {
                _wrapper.SetActive(active);
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = active ? 1 : 0;
                }
            }
        }
    }
}