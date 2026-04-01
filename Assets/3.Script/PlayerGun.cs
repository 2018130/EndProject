using Unity.Netcode;
using UnityEngine;

public class PlayerGun : MonoBehaviour
{
    private BaseWeapon currentWeapon;

    [SerializeField] private Vector3 weaponOffset = new Vector3(0.7f, 0.7f, 0f);

    public void EquipWeapon(BaseWeapon weapon)
    {
        if (currentWeapon != null)
        {
            currentWeapon.transform.SetParent(null);
            currentWeapon = null;
        }
        currentWeapon = weapon;
        currentWeapon.transform.localPosition = weaponOffset;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }

    public BaseWeapon GetCurrentWeapon() => currentWeapon;
}
