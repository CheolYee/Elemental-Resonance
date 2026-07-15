using System;

namespace Gamelib.SoundSystem.Editor
{
    // SoundListSo 연동 없이 검색만 필요한 일반 enum용.
    // 새 enum에 검색기 추가 시 3줄 서브클래스만 작성:
    //   [CustomPropertyDrawer(typeof(MyEnum))]
    //   public sealed class MyEnumDrawer : GenericEnumDrawer<MyEnum> { }
    public abstract class GenericEnumDrawer<T> : EnumSearchDrawerBase<T> where T : Enum { }
}
