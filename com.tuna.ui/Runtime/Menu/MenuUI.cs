using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace Core
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private Button[] _btn;
        [SerializeField] private GameObject[] _popup;
        [SerializeField] private GameObject[] _iconOff;
        [SerializeField] private GameObject[] _iconOn;
        
        [SerializeField] private GameObject _focus;
        [SerializeField] private Vector3 _offsetFocus;
        private Tween _focusTween;
        private int _currentIndex = -1;

        private void Awake()
        {
            for (var i = 0; i < _btn.Length; i++)
            {
                var index = i;
                _btn[i].onClick.AddListener(() => OpenMenu(index));
            }
        }

        private void OpenMenu(int index)
        {
            var isFocusChanged = _currentIndex != index;

            for (var i = 0; i < _popup.Length; i++)
            {
                var isSelected = i == index;
                _popup[i].SetActive(isSelected);
                _iconOff[i].SetActive(isSelected);
                _iconOn[i].SetActive(!isSelected);
            }

            if (!isFocusChanged)
            {
                return;
            }

            _currentIndex = index;
            _focus.transform.position = _btn[index].transform.position + _offsetFocus;
            _focus.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            if (_focusTween.isAlive)
            {
                _focusTween.Stop();
            }

            _focusTween = Tween.Scale(_focus.transform, Vector3.one, 0.2f, Ease.OutBack);
        }
    }
}
