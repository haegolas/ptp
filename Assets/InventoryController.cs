using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System; //아래 리스트 인벤토리아이템을 넣으려면 유징을 만들어야됨
// MonoBehaviour를 상속받아 게임 오브젝트에 컴포넌트로 붙일 수 있는 인벤토리 메인 스크립트입니다.
public class NewMonoBehaviourScript : MonoBehaviour
{
    //유니티 깃허브 연동 확인중ㅋㅋ
    // ==========================================
    // [1. 변수 선언 영역]
    // ==========================================
    PanelRenderer renderer; // UXML을 렌더링하는 컴포넌트
    VisualElement slotContainer; // 12개 슬롯이 들어가는 민트색 부모 컨테이너
    Label itemInfoLabel; // 하단 텍스트 정보창
    public int totalSlotCount = 12; // 인벤토리 슬롯 수 (12칸)
    public List<InventoryItem> initItems = new List<InventoryItem>(); // 시작 시 인스펙터에서 넣어줄 기본 아이템 목록

    [Header("합성 결과물 아이콘 등록")]
    public Sprite magicSwordIcon; // 엘릭서 + 검 합성 시 만들어질 마법검의 전용 스프라이트 아이콘
    InventoryItem[] slots; // 실제 인벤토리 12칸 데이터 저장소
    bool isInit = false; // 중복 초기화 방지용 스위치
    VisualElement root; // UXML 최상위 루트 도화지
    VisualElement dragGhostIcon; // 마우스를 따라다니는 반투명 고스트 아이콘

    bool isDragging = false; // 드래그 진행 중 여부
    int dragStartIndex = -1; // 드래그 시작 슬롯 번호
    int dragEndIndex = -1; // 드롭할 목표 슬롯 번호
    // ==========================================
    // [2. 데이터 초기화]
    // ==========================================
    void InitSlotData()
    {
        if (isInit == true) return;
        slots = new InventoryItem[totalSlotCount]; // 12칸 배열 생성
        for (int i = 0; i < totalSlotCount; i++)
        {
            if (i < initItems.Count && initItems[i] != null)
            {
                slots[i] = initItems[i].Clone(); // 초기 아이템 복제하여 저장
            }
            else
            {
                slots[i] = new InventoryItem(); // 빈칸 생성
            }
        }
        isInit = true;
    }
    void Start()
    {
        InitSlotData();
        renderer = GetComponent<PanelRenderer>();
        renderer.RegisterUIReloadCallback(OnUIReload);
    }
    // ==========================================
    // [3. UI 구성 및 렌더링]
    // ==========================================
    void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        this.root = root;
        slotContainer = root.Q<VisualElement>("SlotContainer");
        itemInfoLabel = root.Q<Label>("ItemInfoLabel");
        CreateDragGhostIcon();
        // 마우스 이동 및 떼기(드롭)는 최상위 root에서 일괄 감지
        root.RegisterCallback<PointerMoveEvent>(OnRootPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
        BuildInventorySlots();
    }
    void CreateDragGhostIcon()
    {
        dragGhostIcon = new VisualElement();
        dragGhostIcon.name = "Ghost";
        dragGhostIcon.style.position = Position.Absolute;
        dragGhostIcon.style.width = 64;
        dragGhostIcon.style.height = 64;
        dragGhostIcon.style.opacity = 0.6f;
        dragGhostIcon.pickingMode = PickingMode.Ignore; // 고스트가 마우스 입력을 가로채지 않도록 무시
        dragGhostIcon.style.display = DisplayStyle.None;
        root.Add(dragGhostIcon);
    }
    void OnRootPointerMove(PointerMoveEvent e)
    {
        if (isDragging == false || dragGhostIcon == null) return;
        // 마우스 커서 중심에 고스트 아이콘 배치
        dragGhostIcon.style.left = e.position.x - 32;
        dragGhostIcon.style.top = e.position.y - 32;
    }
    void BuildInventorySlots()
    {
        if (slotContainer == null) return;
        slotContainer.Clear(); // 슬롯들을 지우고 새로고침
        for (int i = 0; i < totalSlotCount; i++)
        {
            VisualElement slotElement = new VisualElement();
            slotElement.name = "Slot_" + i;
            slotElement.AddToClassList("inventory-slot");
            slotElement.userData = i; // 슬롯 번호 저장
            slotElement.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
            slotElement.RegisterCallback<PointerEnterEvent>(OnSlotPointerEnter);
            slotElement.RegisterCallback<PointerLeaveEvent>(OnSlotPointerLeave);
            InventoryItem currentItem = slots[i];
            if (currentItem.IsEmpty() == false)
            {
                // 아이콘 생성
                VisualElement iconElement = new VisualElement();
                iconElement.AddToClassList("slot-icon");
                iconElement.pickingMode = PickingMode.Ignore;
                if (currentItem.icon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(currentItem.icon);
                }
                slotElement.Add(iconElement);
                // 수량 뱃지 생성 (장비 아이템이라도 수량이 1개 이상이면 표시)
                Label currentLabel = new Label("" + currentItem.amount);
                currentLabel.AddToClassList("slot-label");
                currentLabel.pickingMode = PickingMode.Ignore;
                slotElement.Add(currentLabel);
            }
            slotContainer.Add(slotElement);
        }
    }
    // ==========================================
    // [4. 드래그 앤 드롭 & 합성 & 강화 로직]
    // ==========================================
    void OnSlotPointerEnter(PointerEnterEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if (slot != null && slot.userData != null)
        {
            dragEndIndex = (int)slot.userData; // 마우스가 진입한 슬롯 번호 기억
        }
    }
    void OnSlotPointerLeave(PointerLeaveEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if (slot != null && slot.userData != null)
        {
            int index = (int)slot.userData;
            if (dragEndIndex == index)
            {
                dragEndIndex = -1; // 슬롯 바깥으로 나가면 목적지 취소
            }
        }
    }
    // 마우스를 클릭할 때 (드래그 시작)
    void OnSlotPointerDown(PointerDownEvent e)
    {
        VisualElement clickedSlot = (VisualElement)e.currentTarget;
        if (clickedSlot == null || clickedSlot.userData == null) return;
        int slotIndex = (int)clickedSlot.userData;
        if (e.button == 0) // 좌클릭
        {
            ShowItemInfo(slotIndex); // 아이템 정보 출력
            if (slots[slotIndex].IsEmpty() == false)
            {
                isDragging = true;
                dragStartIndex = slotIndex;
                dragEndIndex = slotIndex;
                // 마우스가 화면 밖으로 나가도 절대 놓치지 않도록 포인터 캡처 실행!
                root.CapturePointer(e.pointerId);
                dragGhostIcon.style.backgroundImage = new StyleBackground(slots[slotIndex].icon);
                dragGhostIcon.style.display = DisplayStyle.Flex;
                dragGhostIcon.style.left = e.position.x - 32;
                dragGhostIcon.style.top = e.position.y - 32;
            }
        }
        else if (e.button == 1) // 우클릭 (사용)
        {
            UseItem(slotIndex);
            BuildInventorySlots();
        }
    }
    // 마우스를 뗐을 때 (드래그 종료: 합성, 강화, 스왑, 또는 버리기!)
    void OnSlotPointerUp(PointerUpEvent e)
    {
        if (isDragging == false) return;
        isDragging = false;
        // 잡고 있던 마우스 포인터 캡처 해제
        if (root.HasPointerCapture(e.pointerId))
        {
            root.ReleasePointer(e.pointerId);
        }
        if (dragGhostIcon != null)
        {
            dragGhostIcon.style.display = DisplayStyle.None; // 고스트 숨기기
        }
        // 레이캐스트 안전장치: 마우스 바로 아래의 슬롯 감지
        if (dragEndIndex == -1)
        {
            VisualElement picked = root.panel.Pick(e.position);
            while (picked != null)
            {
                if (picked.ClassListContains("inventory-slot") && picked.userData != null)
                {
                    dragEndIndex = (int)picked.userData;
                    break;
                }
                picked = picked.parent;
            }
        }
        // ----------------------------------------------------
        // [경우의 수 1: 인벤토리 바깥으로 드래그하여 버렸을 때!]
        // ----------------------------------------------------
        if (dragEndIndex == -1)
        {
            InventoryItem droppedItem = slots[dragStartIndex];
            itemInfoLabel.text = $"🗑️ [{droppedItem.name}] 아이템을 인벤토리 밖으로 버렸습니다!";

            slots[dragStartIndex].Clear(); // 해당 슬롯을 완전히 비움 (소멸)
            BuildInventorySlots(); // 화면 새로고침
        }
        // ----------------------------------------------------
        // [경우의 수 2: 정상적인 슬롯 위에 올려놓았을 때]
        // ----------------------------------------------------
        else if (dragEndIndex >= 0 && dragEndIndex < totalSlotCount && dragEndIndex != dragStartIndex)
        {
            InventoryItem startItem = slots[dragStartIndex];
            InventoryItem endItem = slots[dragEndIndex];
            // ★ [신규 업그레이드!] 합성 및 강화 이벤트 검사! (엘릭서 + 마법검 = 마법검 강화!)
            if (TryCombineOrUpgradeItems(dragStartIndex, dragEndIndex))
            {
                // 합성이나 강화에 성공했으면 끝! (함수 내부에서 메시지와 화면 갱신 완료)
            }
            // 2-1. 같은 아이템이고 겹칠 수 있는 경우 (수량 합치기)
            else if (startItem.IsSameItem(endItem) && endItem.maxStack > 1)
            {
                int canAddAmount = endItem.maxStack - endItem.amount;
                if (canAddAmount >= startItem.amount)
                {
                    endItem.amount += startItem.amount;
                    startItem.Clear();
                    itemInfoLabel.text = $"[{startItem.name}] 아이템을 {(dragEndIndex + 1)}번 슬롯에 모두 합쳤습니다. (총 {endItem.amount}개)";
                }
                else
                {
                    endItem.amount = endItem.maxStack;
                    startItem.amount -= canAddAmount;
                    itemInfoLabel.text = $"[{startItem.name}] 아이템을 {(dragEndIndex + 1)}번 슬롯에 채웠습니다. (남은 수량: {startItem.amount}개)";
                }
                BuildInventorySlots();
            }
            // 2-2. 목적지가 빈칸인 경우 (단순 이동)
            else if (endItem.IsEmpty() == true)
            {
                slots[dragEndIndex] = startItem;
                slots[dragStartIndex] = endItem;
                itemInfoLabel.text = $"[{startItem.name}] 아이템을 {(dragEndIndex + 1)}번 슬롯으로 이동했습니다.";
                BuildInventorySlots();
            }
            // 2-3. 다른 아이템인 경우 (자리 맞바꾸기 / 스왑)
            else
            {
                string startName = startItem.name;
                string endName = endItem.name;
                slots[dragStartIndex] = endItem;
                slots[dragEndIndex] = startItem;
                itemInfoLabel.text = $"[{(dragStartIndex + 1)}번 {startName}]과 [{(dragEndIndex + 1)}번 {endName}]의 자리를 맞바꿨습니다.";
                BuildInventorySlots();
            }
        }
        // 드래그 상태 초기화
        dragStartIndex = -1;
        dragEndIndex = -1;
    }
    // ==========================================
    // [5. 아이템 합성 & 무기 강화(Enchant) 시스템]
    // ==========================================
    // 합성 또는 강화를 판별하여 처리하는 메서드
    bool TryCombineOrUpgradeItems(int fromIndex, int toIndex)
    {
        InventoryItem itemA = slots[fromIndex];
        InventoryItem itemB = slots[toIndex];
        if (itemA.IsEmpty() || itemB.IsEmpty()) return false;
        // 재료 중에 '엘릭서/포션'이 있는지 확인
        bool aIsPotion = itemA.name.Contains("엘릭서") || itemA.name.Contains("포션");
        bool bIsPotion = itemB.name.Contains("엘릭서") || itemB.name.Contains("포션");
        // 재료 중에 '검/소드'가 있는지 확인
        bool aIsSword = itemA.name.Contains("검") || itemA.name.Contains("소드");
        bool bIsSword = itemB.name.Contains("검") || itemB.name.Contains("소드");
        // [엘릭서 + 검] 조합일 때만 작동! (어느 쪽을 드래그하든 허용)
        if ((aIsPotion && bIsSword) || (bIsPotion && aIsSword))
        {
            // 재료 중 엘릭서와 검을 정확히 구분하기
            int potionIndex = aIsPotion ? fromIndex : toIndex;
            int swordIndex = aIsSword ? fromIndex : toIndex;
            InventoryItem potionItem = slots[potionIndex];
            InventoryItem swordItem = slots[swordIndex];
            // ----------------------------------------------------
            // ★ [단계 B: 이미 마법검인 경우 ➔ 강화(Upgrade) 실행!]
            // ----------------------------------------------------
            if (swordItem.name.Contains("마법검"))
            {
                // 1. 엘릭서 수량 1개 차감
                potionItem.amount -= 1;
                if (potionItem.amount <= 0) potionItem.Clear();
                // 2. 마법검 강화 수치 증가!
                swordItem.upgradeLevel += 1; // +1, +2, +3...
                swordItem.name = $"마법검 +{swordItem.upgradeLevel}";

                // 공격력 수치 계산 (기본 50 + 강화당 25씩 증가)
                int totalDamage = 50 + (swordItem.upgradeLevel * 25);
                swordItem.description = $"엘릭서의 마력이 {swordItem.upgradeLevel}번 중첩 주입된 검! (공격력 +{totalDamage})";
                // 3. 화려한 강화 성공 연출 메시지!
                itemInfoLabel.text = $"⚡ [강화 대성공!] 마력이 응축되어 [{swordItem.name}]이 탄생했습니다! (공격력 +{totalDamage})";
            }
            // ----------------------------------------------------
            // ★ [단계 A: 일반 검인 경우 ➔ 최초 [마법검] 탄생!]
            // ----------------------------------------------------
            else
            {
                // 1. 엘릭서 수량 1개 차감
                potionItem.amount -= 1;
                if (potionItem.amount <= 0) potionItem.Clear();
                // 2. 일반 검을 마법검으로 진화!
                Sprite resultIcon = (magicSwordIcon != null) ? magicSwordIcon : swordItem.icon;
                swordItem.name = "마법검 +1";
                swordItem.upgradeLevel = 1;
                swordItem.description = "엘릭서의 마력이 깃들어 푸른빛 오라를 뿜는 검! (공격력 +75)";
                swordItem.icon = resultIcon;
                itemInfoLabel.text = "✨ [진화 성공!] 평범한 검에 마력이 주입되어 [마법검 +1]로 각성했습니다!";
            }
            // 목적지 슬롯이 방금 변환된 검이 오도록 보정
            if (toIndex != swordIndex)
            {
                slots[toIndex] = swordItem;
                slots[swordIndex].Clear();
            }
            BuildInventorySlots(); // 화면 새로고침
            return true; // 합성/강화 성공!
        }
        return false; // 해당 없음
    }
    // ==========================================
    // [6. 정보창 및 사용]
    // ==========================================
    void ShowItemInfo(int index)
    {
        InventoryItem clickedItem = slots[index];
        if (clickedItem.IsEmpty() == true)
        {
            itemInfoLabel.text = (index + 1) + "번 슬롯은 비어있습니다.";
        }
        else
        {
            itemInfoLabel.text = "[" + clickedItem.name + "] " + clickedItem.description;
        }
    }
    void UseItem(int index)
    {
        InventoryItem item = slots[index];
        if (item.IsEmpty() == true)
        {
            itemInfoLabel.text = (index + 1) + "번 슬롯에는 사용할 아이템이 없습니다.";
            return;
        }
        if (item.maxStack <= 1)
        {
            itemInfoLabel.text = "[" + item.name + "] 아이템은 소모품이 아닙니다.";
            return;
        }
        string usedItem = item.name;
        item.amount -= 1;
        if (item.amount <= 0)
        {
            item.Clear();
            itemInfoLabel.text = "[" + usedItem + "] 모두 소모하였습니다.";
        }
        else
        {
            itemInfoLabel.text = "[" + usedItem + "] 아이템 사용! 남은 수량(" + item.amount + "개)";
        }
    }
}
// ==========================================
// [7. 아이템 데이터 모델]
// ==========================================
[Serializable]
public class InventoryItem
{
    public string name;
    public string description;
    public Sprite icon;
    public int amount;
    public int maxStack;
    public int upgradeLevel = 0; // ★ [신규!] 강화 수치를 기억하는 변수 (+1, +2, +3...)
    public InventoryItem()
    {
        Clear();
    }
    public InventoryItem(string a, string b, Sprite c, int d, int e, int level = 0)
    {
        name = a;
        description = b;
        icon = c;
        amount = d;
        maxStack = e;
        upgradeLevel = level;
    }
    public void Clear()
    {
        name = "";
        description = "";
        icon = null;
        amount = 0;
        maxStack = 0;
        upgradeLevel = 0;
    }
    public InventoryItem Clone()
    {
        return new InventoryItem(name, description, icon, amount, maxStack, upgradeLevel);
    }
    public bool IsEmpty()
    {
        if (string.IsNullOrEmpty(name) || amount <= 0)
        {
            return true;
        }
        return false;
    }
    public bool IsSameItem(InventoryItem other)
    {
        if (other == null) return false;
        if (this.IsEmpty() || other.IsEmpty()) return false;
        // 이름과 강화 수치까지 같아야 같은 아이템으로 취급
        return this.name == other.name && this.upgradeLevel == other.upgradeLevel;
    }
}