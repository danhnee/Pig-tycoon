using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Điểm chuyển dịch bản đồ (Map Transition Portal) giữa Nông Trại của Khoa và Làng NPC Pelican.
    /// </summary>
    [SelectionBase]
    public class VillageTravelTrigger : MonoBehaviour
    {
        public enum DestinationType
        {
            ToNPCVillage,
            ToFarm
        }

        public DestinationType TargetDestination;
        public string PromptText = "Đi đến Làng NPC";

        public void Travel()
        {
            var player = PlayerInteractionController.Instance;
            if (player == null) return;

            var camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;

            if (TargetDestination == DestinationType.ToNPCVillage)
            {
                // Teleport sang Cổng Nam của Làng Pelican (đặt chân ngay trên con đường gỗ bước vào làng)
                Vector3 dest = new Vector3(195.2f, -221.5f, 0f);
                player.transform.position = dest;

                if (camFollow != null)
                {
                    // Ranh giới Camera khớp chuẩn xác toàn bộ bản đồ Pelican Town (1024x866 px @ PPU 16)
                    camFollow.CustomBounds = new Rect(168f, -227.5f, 64f, 55f);
                    camFollow.transform.position = new Vector3(dest.x, dest.y, camFollow.transform.position.z);
                }

                player.ShowFeedback("\u2728 \u0110\u00E3 \u0111\u1EBFn Th\u1ECB tr\u1EA5n Pelican (L\u00E0ng NPC)! H\u00E3y kh\u00E1m ph\u00E1 qu\u1EA3ng tr\u01B0\u1EDDng, c\u00E1c ti\u1EC7m ph\u1ED1 v\u00E0 b\u1EDD s\u00F4ng.");
            }
            else
            {
                // Teleport quay về Cổng Nam Nông Trại của Khoa
                Vector3 dest = new Vector3(0f, -31.5f, 0f);
                player.transform.position = dest;

                if (camFollow != null)
                {
                    // Trả lại camera cho toàn bộ Nông Trại
                    camFollow.CustomBounds = null;
                    camFollow.transform.position = new Vector3(dest.x, dest.y, camFollow.transform.position.z);
                }

                player.ShowFeedback("\uD83C\uDFE1 \u0110\u00E3 tr\u1EDF v\u1EC1 N\u00F4ng Tr\u1EA1i c\u1EE7a Khoa!");
            }
        }
    }

    /// <summary>
    /// NPC dân làng Pelican có thể tương tác trò chuyện, tặng quà hoặc hỗ trợ thể lực.
    /// Có hiệu ứng thở nhẹ nhàng (Breathing idle) và bong bóng trò chuyện (Chat bubble) nhảy nhót khi người chơi lại gần.
    /// </summary>
    [SelectionBase]
    public class VillageNPCView : MonoBehaviour
    {
        public string NPCName = "Dân Làng";
        public string Title = "Dân Làng Thân Thiện";
        [TextArea(2, 4)]
        public string[] Dialogues;
        private int dialogueIndex = 0;

        private Transform speechBubble;
        private Vector3 initialScale;
        private float floatPhase;

        private void Start()
        {
            initialScale = transform.localScale;
            floatPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            CreateSpeechBubble();
        }

        private void CreateSpeechBubble()
        {
            var bubbleObj = new GameObject("ChatBubbleIndicator");
            bubbleObj.transform.SetParent(transform, false);
            bubbleObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            bubbleObj.transform.localScale = Vector3.zero;

            var sr = bubbleObj.AddComponent<SpriteRenderer>();
            sr.sprite = NPCVillageAtmosphere.GetChatBubbleSprite();
            sr.sortingOrder = 9999;
            speechBubble = bubbleObj.transform;
        }

        private void Update()
        {
            // Nhịp thở nhấp nhô nhẹ nhàng tự nhiên
            float breathe = 1f + 0.025f * Mathf.Sin(Time.time * 2.8f + floatPhase);
            transform.localScale = new Vector3(initialScale.x * breathe, initialScale.y * (2f - breathe), initialScale.z);

            // Bong bóng đối thoại hiện lên khi Khoa lại gần trong phạm vi 2.8m
            var player = PlayerInteractionController.Instance;
            if (player != null && speechBubble != null)
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                bool inRange = dist < 2.8f;
                float targetScale = inRange ? 1f : 0f;
                float currentScale = speechBubble.localScale.x;
                float newScale = Mathf.MoveTowards(currentScale, targetScale, Time.deltaTime * 6f);
                speechBubble.localScale = Vector3.one * newScale;

                if (newScale > 0.01f)
                {
                    float bob = Mathf.Sin(Time.time * 4f + floatPhase) * 0.08f;
                    speechBubble.localPosition = new Vector3(0f, 1.45f + bob, 0f);
                }
            }
        }

        public void Talk()
        {
            var player = PlayerInteractionController.Instance;
            var engine = MobileGameController.Instance?.Engine;

            if (Dialogues != null && Dialogues.Length > 0)
            {
                string line = Dialogues[dialogueIndex % Dialogues.Length];
                dialogueIndex++;

                // Bé Linh: Tặng tách trà thảo mộc hồi đầy 100% thể lực
                if (NPCName.Contains("Linh") && engine?.Character?.Stamina != null)
                {
                    engine.Character.Stamina.CurrentStamina = engine.Character.Stamina.MaxStamina;
                }

                player?.ShowFeedback($"[{NPCName}]: \"{line}\"");
            }
            else
            {
                player?.ShowFeedback($"[{NPCName}]: \"Ch\u00E0o Khoa! Ch\u00FAc m\u1ED9t ng\u00E0y t\u1ED1t l\u00E0nh!\"");
            }
        }
    }

    /// <summary>
    /// Xây dựng và quản lý Bản đồ Thị trấn Pelican (Làng NPC) đúng chuẩn hình ảnh Stardew Valley:
    /// - Sử dụng toàn bộ bản đồ gốc 1024x866 pixel art của Pelican Town làm mặt bằng không gian.
    /// - Đầy đủ quảng trường, đài phun nước, tiệm bách hóa Pierre, lò rèn Clint, quán rượu Saloon, tòa thị chính, dòng sông & cầu gỗ.
    /// - Hệ thống vật cản (Colliders) kiên cố bao quanh các tòa nhà và bờ sông, cho phép đi lại tự nhiên trên các con đường và cầu.
    /// - 4 nhân vật NPC tương tác phong phú: Cụ Bá, Cô Mai, Bác Hùng, Bé Linh.
    /// </summary>
    public static class NPCVillageManager
    {
        public const string SouthGateRootName = "Farm_South_Gate_Portal";
        public const string VillageRootName = "Map_NPC_Village";

        public static readonly Vector2 VillageCenter = new Vector2(200f, -200f);
        public const float MapPPU = 16f; // Chuẩn 16 pixels per unit của Stardew Valley
        public const float MapWidthUnits = 64.0f; // 1024 / 16
        public const float MapHeightUnits = 54.125f; // 866 / 16

        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static void BuildIfMissing()
        {
            BuildSouthGateAtFarm();
            BuildVillageMap();
        }

        // =========================================================================
        // 1. CỔNG NAM NÔNG TRẠI VÀ ĐƯỜNG DẪN HOÀN CHỈNH
        // =========================================================================
        public static void BuildSouthGateAtFarm()
        {
            var existingGate = GameObject.Find(SouthGateRootName);
            if (existingGate != null)
            {
                existingGate.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(existingGate);
                else UnityEngine.Object.DestroyImmediate(existingGate);
            }

            var root = new GameObject(SouthGateRootName);
            root.transform.position = Vector3.zero;

            // 1. Thảm đường đất lát gạch sạch sẽ dẫn từ nông trại xuống tận cổng (Y: -26 -> -34.5, X: 0, rộng 2.5m)
            var farmRoadObj = new GameObject("Farm_South_Road_Pavement");
            farmRoadObj.transform.SetParent(root.transform, false);
            farmRoadObj.transform.position = new Vector3(0f, -30.5f, 0f);
            var farmRoadSr = farmRoadObj.AddComponent<SpriteRenderer>();
            farmRoadSr.sprite = GetDirtRoadSprite();
            farmRoadSr.drawMode = SpriteDrawMode.Tiled;
            farmRoadSr.tileMode = SpriteTileMode.Continuous;
            farmRoadSr.size = new Vector2(2.5f, 8.0f);
            farmRoadSr.sortingOrder = -9850;

            // 2. Hai cột đèn lồng đá cổ kính hai bên cổng
            CreateStoneLanternPost(root.transform, new Vector3(-1.6f, -33.5f, 0f));
            CreateStoneLanternPost(root.transform, new Vector3(1.6f, -33.5f, 0f));

            // 3. Biển báo qua map mới Làng NPC đặt bên phải đường (X: 1.8f, Y: -32.5f)
            var signObj = new GameObject("Signpost_To_NPCVillage");
            signObj.transform.SetParent(root.transform, false);
            signObj.transform.position = new Vector3(1.8f, -32.5f, 0f);
            signObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var sr = signObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetSignpostSprite(true);
            sr.sortingOrder = Mathf.RoundToInt(-(-32.5f) * 100);

            var col = signObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.0f);
            col.isTrigger = true;

            var trigger = signObj.AddComponent<VillageTravelTrigger>();
            trigger.TargetDestination = VillageTravelTrigger.DestinationType.ToNPCVillage;
            trigger.PromptText = "\u0110i \u0111\u1EBFn L\u00E0ng NPC Pelican";
        }

        // =========================================================================
        // 2. XÂY DỰNG TOÀN BỘ BẢN ĐỒ THỊ TRẤN PELICAN CHUẨN XÁC 100% THEO ẢNH
        // =========================================================================
        public static void BuildVillageMap()
        {
            var existingVillage = GameObject.Find(VillageRootName);
            if (existingVillage != null)
            {
                existingVillage.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(existingVillage);
                else UnityEngine.Object.DestroyImmediate(existingVillage);
            }

            var root = new GameObject(VillageRootName);
            root.transform.position = Vector3.zero;

            Vector2 center = VillageCenter;

            // 1. TẢI VÀ HIỂN THỊ TOÀN BỘ BẢN ĐỒ PELICAN TOWN 1024x866 PIXEL ART GỐC
            var mapObj = new GameObject("Pelican_Town_Full_Map");
            mapObj.transform.SetParent(root.transform, false);
            mapObj.transform.position = new Vector3(center.x, center.y, 0f);

            var mapSr = mapObj.AddComponent<SpriteRenderer>();
            mapSr.sprite = GetPelicanTownMapSprite();
            mapSr.sortingOrder = -10000; // Đặt làm nền móng cơ sở cho toàn bộ thị trấn

            // 2. THIẾT LẬP VẬT CẢN (COLLIDERS) KIÊN CỐ KHỚP TỪNG TÒA NHÀ & BỜ SÔNG
            BuildTownColliders(root.transform, center);

            // 3. ĐẶT CÁC NHÂN VẬT NPC VÀO ĐÚNG TỌA ĐỘ TRONG THỊ TRẤN
            BuildTownNPCs(root.transform, center);

            // 4. CỔNG NAM THỊ TRẤN (CẦU PHÍA NAM) & BIỂN BÁO QUAY VỀ FARM
            BuildTownSouthPortal(root.transform, center);

            // 5. HIỆU ỨNG KHÍ QUYỂN THƠ MỘNG: CÁNH HOA ANH ĐÀO BAY, KHÓI LAM CHIỀU, LỬA LÒ RÈN & ĐÈN ĐƯỜNG
            NPCVillageAtmosphere.AttachTo(root.transform, center);
        }

        // --- HỆ THỐNG VẬT CẢN (COLLIDERS) KHỚP CHÍNH XÁC KIẾN TRÚC PELICAN TOWN ---
        private static void BuildTownColliders(Transform parent, Vector2 center)
        {
            var colRoot = new GameObject("Town_Colliders_Layer");
            colRoot.transform.SetParent(parent, false);

            // A. DÒNG SÔNG PELICAN (Ngăn đi dưới nước, mở thông 3 cây cầu chính)
            // 1. Nhánh sông phía Bắc (chạy dọc từ góc trên bên phải xuống ôm lấy Cầu Đá)
            AddSolidBox(colRoot.transform, new Vector2(center.x + 14.5f, center.y + 19.5f), new Vector2(6.0f, 15.5f), "River_North_Upper");
            AddSolidBox(colRoot.transform, new Vector2(center.x + 9.0f, center.y + 9.5f), new Vector2(7.5f, 5.5f), "River_North_Bend");
            // Khúc cua mép nước ngay trên Cầu Đá (ngăn người chơi đi/bơi dưới lòng sông)
            AddSolidBox(colRoot.transform, new Vector2(center.x + 4.0f, center.y + 5.5f), new Vector2(5.5f, 7.5f), "River_North_Turn");

            // 2. Nhanh song giua - Polygon muot ma om sat bo nuoc, mo thong hoan toan duong bo ven song
            Vector2[] midRiverPoints = new Vector2[]
            {
                // Bo Tay (tu Cau Da di xuong)
                new Vector2(center.x + 2.5f, center.y - 0.8f),
                new Vector2(center.x + 2.8f, center.y - 3.0f),
                new Vector2(center.x + 3.8f, center.y - 6.0f),
                new Vector2(center.x + 4.0f, center.y - 9.0f),
                new Vector2(center.x + 3.2f, center.y - 12.0f),
                new Vector2(center.x + 1.0f, center.y - 13.8f),
                // Day noi sang Cau Tiem Ren
                new Vector2(center.x + 5.5f, center.y - 13.8f),
                // Bo Dong (uon luon muot ma di len, mo rong hanh lang di bo >2.5m)
                new Vector2(center.x + 6.0f, center.y - 12.5f),
                new Vector2(center.x + 7.2f, center.y - 11.0f),
                new Vector2(center.x + 8.2f, center.y - 9.5f),
                new Vector2(center.x + 8.8f, center.y - 8.0f),
                new Vector2(center.x + 9.2f, center.y - 6.5f),
                new Vector2(center.x + 9.2f, center.y - 4.5f),
                new Vector2(center.x + 8.5f, center.y - 3.0f),
                new Vector2(center.x + 7.5f, center.y - 2.0f),
                new Vector2(center.x + 5.8f, center.y - 0.8f)
            };
            AddSolidPolygon(colRoot.transform, midRiverPoints, "River_Mid_Polygon");

            // 3. Nhánh sông phía Nam (từ dưới Cầu Tiệm Rèn uốn sang cửa vịnh Tây Nam)
            // CẦU GỖ TIỆM RÈN (Plank Bridge sang Lò Rèn Clint): [X: center.x + 1.0 -> center.x + 7.0], [Y: center.y - 15.2 -> center.y - 13.2] HOÀN TOÀN THÔNG THOÁNG!
            AddSolidBox(colRoot.transform, new Vector2(center.x + 5.5f, center.y - 18.0f), new Vector2(6.0f, 5.5f), "River_South_Bend");
            // Đoạn sông phía Nam - Phía Đông Cầu Nam
            AddSolidBox(colRoot.transform, new Vector2(center.x + 4.5f, center.y - 24.0f), new Vector2(15.0f, 4.0f), "River_South_East_Bay");
            // Đoạn sông phía Nam - Phía Tây Cầu Nam
            // CỔNG NAM / CẦU CỔNG NAM VỀ NÔNG TRẠI: [X: center.x - 7.0 -> center.x - 3.0], [Y: center.y - 26.5 -> center.y - 22.0] HOÀN TOÀN THÔNG THOÁNG!
            AddSolidBox(colRoot.transform, new Vector2(center.x - 19.5f, center.y - 24.0f), new Vector2(24.0f, 4.0f), "River_South_West_Bay");

            // B. CÁC TÒA NHÀ & CÔNG TRÌNH KIẾN TRÚC CHÍNH (Tọa độ khớp chính xác 100% pixel art)
            // 1. Tòa Thị Chính / Community Center (Phía Bắc - chừa hoàn toàn cầu thang & con đường phía Đông)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 12.5f, center.y + 18.5f), new Vector2(13.0f, 7.5f), "Building_CommunityCenter");
            // 2. Tiệm Bách Hóa Pierre & Phòng Khám Clinic (Khu Tây - chừa con đường đất chạy dọc)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 13.5f, center.y + 3.0f), new Vector2(13.0f, 6.5f), "Building_PierresStore_Clinic");
            // 3. Quán Rượu The Stardrop Saloon (Khu Trung Tâm Tây)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 11.4f, center.y - 5.5f), new Vector2(7.5f, 6.0f), "Building_Saloon");
            // 4. Biệt Thự Trưởng Làng Mayor Lewis (Khu Nam Trung Tâm)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 5.8f, center.y - 9.8f), new Vector2(7.0f, 6.5f), "Building_LewisManor");
            // 5. Nhà của Evelyn, George & Alex (Phía Đông Quảng Trường)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 4.5f, center.y - 2.5f), new Vector2(7.5f, 6.0f), "Building_EvelynHouse");
            // 6. Nhà của Haley & Emily - 2 Willow Lane (Góc Tây Nam)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 25.5f, center.y - 14.0f), new Vector2(7.5f, 6.5f), "Building_HaleyHouse");
            // 7. Nhà của Jodi, Sam & Vincent - 1 Willow Lane (Khu Tây Nam)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 20.0f, center.y - 16.0f), new Vector2(7.5f, 6.5f), "Building_JodiHouse");
            // 8. Nhà xe Mooc / Trailer của Pam & Penny (Phía Nam nhà Evelyn, không vướng Cầu Đá)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 0.5f, center.y - 6.5f), new Vector2(6.0f, 4.5f), "Building_PamTrailer");
            // 9. Siêu thị JojaMart / Rạp Chiếu Phim Movie Theater (Bên kia sông phía Đông Bắc)
            AddSolidBox(colRoot.transform, new Vector2(center.x + 19.5f, center.y + 3.5f), new Vector2(13.0f, 8.0f), "Building_Joja_MovieTheater");
            // 10. Tiem Lo Ren Clint (Ben kia song phia Dong - can le X >= 213.0, mo rong duong ven song)

            AddSolidBox(colRoot.transform, new Vector2(center.x + 16.0f, center.y - 10.0f), new Vector2(6.0f, 6.5f), "Building_Blacksmith");
            // San vuon cay anh dao phia Bac Lo Ren Clint (le rao X >= 212.0)
            AddSolidBox(colRoot.transform, new Vector2(center.x + 15.5f, center.y - 3.8f), new Vector2(7.0f, 5.5f), "Prop_CherryBlossomYard");
            // 11. Viện Bảo Tàng & Thư Viện Gunther (Góc Đông Nam)
            AddSolidBox(colRoot.transform, new Vector2(center.x + 17.7f, center.y - 14.5f), new Vector2(9.5f, 6.5f), "Building_Museum");

            // C. CÁC CHƯỚNG NGẠI VẬT, VẬT DỤNG, BỜ RÀO & CẢNH QUAN TRONG THỊ TRẤN
            // 1. Đài phun nước trung tâm (Fountain Plaza)
            AddSolidCircle(colRoot.transform, new Vector2(center.x - 7.0f, center.y + 2.5f), 1.35f, "Prop_Fountain");
            // 2. Khu vui chơi trẻ em (Playground)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 19.0f, center.y + 23.5f), new Vector2(7.0f, 3.5f), "Prop_Playground");
            // 3. Chiếc xe tải màu xanh của Trưởng Làng Lewis (đậu trước nhà Lewis)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 2.5f, center.y - 12.0f), new Vector2(2.8f, 1.8f), "Prop_LewisTruck");
            // 4. Chuồng chó Dusty (Doghouse & Fence)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 7.5f, center.y - 4.0f), new Vector2(2.5f, 2.0f), "Prop_DogPen");
            // 5. Khu nghĩa trang cổ kính (Cemetery & Tombstones)
            AddSolidBox(colRoot.transform, new Vector2(center.x - 15.0f, center.y - 1.5f), new Vector2(3.5f, 3.0f), "Prop_Graveyard");
            // 6. Hàng rào vườn rau nhà Evelyn
            AddSolidBox(colRoot.transform, new Vector2(center.x - 1.2f, center.y - 4.0f), new Vector2(3.0f, 2.5f), "Prop_EvelynGarden");
            // 7. Hàng rào sân sau nhà Jodi
            AddSolidBox(colRoot.transform, new Vector2(center.x - 23.0f, center.y - 19.0f), new Vector2(4.5f, 2.5f), "Prop_JodiBackyard");

            // D. VÁCH NÚI, ĐỊA HÌNH TRẮC TRỞ & BỜ ĐÁ
            AddSolidBox(colRoot.transform, new Vector2(center.x - 21.0f, center.y + 23.5f), new Vector2(13.0f, 2.5f), "Cliff_North_West");
            AddSolidBox(colRoot.transform, new Vector2(center.x + 7.5f, center.y + 23.5f), new Vector2(14.0f, 2.5f), "Cliff_North_East");
            AddSolidBox(colRoot.transform, new Vector2(center.x + 23.0f, center.y + 20.0f), new Vector2(9.0f, 8.0f), "Cliff_Joja_Top");
            AddSolidBox(colRoot.transform, new Vector2(center.x + 23.0f, center.y + 5.0f), new Vector2(4.0f, 14.0f), "Cliff_East_Quarry");
            AddSolidBox(colRoot.transform, new Vector2(center.x + 24.0f, center.y - 8.0f), new Vector2(4.5f, 7.0f), "Cliff_Blacksmith_Rocks");

            // E. RANH GIỚI VÒNG NGOÀI THỊ TRẤN PELICAN
            AddSolidBox(colRoot.transform, new Vector2(center.x - 32.5f, center.y), new Vector2(1.5f, 55f), "Boundary_West");
            AddSolidBox(colRoot.transform, new Vector2(center.x + 32.5f, center.y), new Vector2(1.5f, 55f), "Boundary_East");
            AddSolidBox(colRoot.transform, new Vector2(center.x, center.y + 27.5f), new Vector2(64f, 1.5f), "Boundary_North");
            AddSolidBox(colRoot.transform, new Vector2(center.x - 19.5f, center.y - 27.5f), new Vector2(25f, 1.5f), "Boundary_South_West");
            AddSolidBox(colRoot.transform, new Vector2(center.x + 14.5f, center.y - 27.5f), new Vector2(35f, 1.5f), "Boundary_South_East");

            // F. TOÀN BỘ GỐC CÂY CỐI TRONG THỊ TRẤN (Đã làm sạch hoàn toàn khỏi đường đi, cầu và sông)
            for (int i = 0; i < TownTreePositions.Length; i++)
            {
                AddSolidCircle(colRoot.transform, TownTreePositions[i], 0.45f, $"Tree_Trunk_{i + 1}");
            }
        }

        private static void AddSolidBox(Transform parent, Vector2 pos, Vector2 size, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = false;
        }

        private static void AddSolidCircle(Transform parent, Vector2 pos, float radius, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = radius;
            col.isTrigger = false;
        }

        private static void AddSolidPolygon(Transform parent, Vector2[] worldPoints, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;
            var col = go.AddComponent<PolygonCollider2D>();
            col.points = worldPoints;
            col.isTrigger = false;
        }

        // --- TỌA ĐỘ 191 GỐC CÂY PELICAN TOWN (ĐÃ LOẠI TRỪ TOÀN BỘ ĐƯỜNG ĐI, CẦU VÀ QUẢNG TRƯỜNG) ---
private static readonly Vector2[] TownTreePositions = new Vector2[] {
            new Vector2(171.69f, -177.95f),
            new Vector2(182.27f, -178.25f),
            new Vector2(199.68f, -178.02f),
            new Vector2(203.84f, -177.67f),
            new Vector2(218.04f, -178.1f),
            new Vector2(221.44f, -178.05f),
            new Vector2(224.26f, -178.06f),
            new Vector2(227.19f, -178.56f),
            new Vector2(175f, -179.11f),
            new Vector2(184.88f, -179.44f),
            new Vector2(188.37f, -179.1f),
            new Vector2(193f, -179.27f),
            new Vector2(195.47f, -179.19f),
            new Vector2(197.35f, -179.84f),
            new Vector2(201.8f, -179.59f),
            new Vector2(181.92f, -180.12f),
            new Vector2(219.4f, -179.95f),
            new Vector2(172.23f, -181.1f),
            new Vector2(186.34f, -180.83f),
            new Vector2(188.83f, -180.87f),
            new Vector2(206.08f, -180.86f),
            new Vector2(225.4f, -180.81f),
            new Vector2(183.36f, -181.71f),
            new Vector2(203.56f, -181.85f),
            new Vector2(227.39f, -181.43f),
            new Vector2(176.41f, -182.47f),
            new Vector2(179.2f, -182.4f),
            new Vector2(221.49f, -181.79f),
            new Vector2(224.43f, -182.92f),
            new Vector2(181.94f, -183.08f),
            new Vector2(197.66f, -182.98f),
            new Vector2(200.38f, -182.86f),
            new Vector2(205.14f, -183.35f),
            new Vector2(184.79f, -183.53f),
            new Vector2(190.6f, -183.84f),
            new Vector2(218.27f, -183.5f),
            new Vector2(171.8f, -184.17f),
            new Vector2(219.38f, -184.92f),
            new Vector2(227.18f, -184.28f),
            new Vector2(175.27f, -185.09f),
            new Vector2(180.44f, -184.73f),
            new Vector2(197.91f, -185.78f),
            new Vector2(200.65f, -186f),
            new Vector2(173.21f, -186.12f),
            new Vector2(184.83f, -186.65f),
            new Vector2(187.21f, -186.89f),
            new Vector2(191.2f, -186.71f),
            new Vector2(227.56f, -186.97f),
            new Vector2(176.87f, -187.72f),
            new Vector2(172.25f, -188.58f),
            new Vector2(189.32f, -188.64f),
            new Vector2(184.28f, -189.92f),
            new Vector2(178.66f, -190.04f),
            new Vector2(181.77f, -190.45f),
            new Vector2(187.01f, -190.33f),
            new Vector2(222.43f, -189.93f),
            new Vector2(227.07f, -190.15f),
            new Vector2(173.22f, -190.55f),
            new Vector2(189.28f, -190.57f),
            new Vector2(197.81f, -191.35f),
            new Vector2(200.86f, -191.22f),
            new Vector2(219.02f, -191.11f),
            new Vector2(177.39f, -191.37f),
            new Vector2(224.68f, -191.74f),
            new Vector2(190.61f, -191.89f),
            new Vector2(172.11f, -193.02f),
            new Vector2(174.64f, -192.96f),
            new Vector2(181.6f, -192.91f),
            new Vector2(185.43f, -193f),
            new Vector2(226.22f, -193.01f),
            new Vector2(198.79f, -194.08f),
            new Vector2(176.66f, -194.67f),
            new Vector2(182.24f, -194.83f),
            new Vector2(185.23f, -194.99f),
            new Vector2(188.03f, -195.02f),
            new Vector2(190.88f, -195.1f),
            new Vector2(227.36f, -195.33f),
            new Vector2(219.87f, -195.96f),
            new Vector2(221.68f, -195.84f),
            new Vector2(224.94f, -195.94f),
            new Vector2(172.7f, -196.02f),
            new Vector2(197.68f, -196.92f),
            new Vector2(199.86f, -197.07f),
            new Vector2(171.65f, -197.52f),
            new Vector2(211.82f, -197.8f),
            new Vector2(223.63f, -197.76f),
            new Vector2(182.81f, -198.45f),
            new Vector2(176.96f, -198.88f),
            new Vector2(225.91f, -198.52f),
            new Vector2(198.04f, -198.9f),
            new Vector2(221.2f, -199.13f),
            new Vector2(223.1f, -199.59f),
            new Vector2(227.26f, -199.8f),
            new Vector2(218.64f, -201.85f),
            new Vector2(225.74f, -202.05f),
            new Vector2(220.77f, -202.39f),
            new Vector2(223.39f, -202.21f),
            new Vector2(215.31f, -203.22f),
            new Vector2(176.35f, -203.53f),
            new Vector2(199.54f, -203.91f),
            new Vector2(225.63f, -203.92f),
            new Vector2(177.84f, -204.68f),
            new Vector2(171.52f, -205.03f),
            new Vector2(218.03f, -204.77f),
            new Vector2(219.98f, -204.89f),
            new Vector2(223.11f, -205.05f),
            new Vector2(199.06f, -205.8f),
            new Vector2(216.14f, -206.84f),
            new Vector2(221.62f, -206.56f),
            new Vector2(227.23f, -206.81f),
            new Vector2(201.7f, -207.06f),
            new Vector2(213.88f, -206.9f),
            new Vector2(177.6f, -207.38f),
            new Vector2(171.78f, -207.98f),
            new Vector2(223.69f, -208.6f),
            new Vector2(175.72f, -208.6f),
            new Vector2(187.33f, -208.43f),
            new Vector2(217.14f, -208.51f),
            new Vector2(220.74f, -209.62f),
            new Vector2(225.93f, -209.58f),
            new Vector2(222.86f, -210.33f),
            new Vector2(183.83f, -211.92f),
            new Vector2(188.56f, -211.68f),
            new Vector2(200.32f, -211.9f),
            new Vector2(180.9f, -212.53f),
            new Vector2(220.88f, -212.43f),
            new Vector2(227.06f, -212.35f),
            new Vector2(184.88f, -213.46f),
            new Vector2(223.61f, -213.63f),
            new Vector2(226.98f, -214.81f),
            new Vector2(188.4f, -215.17f),
            new Vector2(193.36f, -215.55f),
            new Vector2(201.26f, -215.51f),
            new Vector2(183.27f, -216.28f),
            new Vector2(185.39f, -216.11f),
            new Vector2(190.33f, -216.13f),
            new Vector2(221.88f, -216.1f),
            new Vector2(201.42f, -217.81f),
            new Vector2(227.39f, -217.59f),
            new Vector2(188.68f, -217.94f),
            new Vector2(190.62f, -218.3f),
            new Vector2(225.31f, -218.27f),
            new Vector2(215.49f, -218.24f),
            new Vector2(192.57f, -219.03f),
            new Vector2(219.07f, -219.54f),
            new Vector2(221.78f, -219.16f),
            new Vector2(183.48f, -220.55f),
            new Vector2(186.89f, -221.05f),
            new Vector2(223.13f, -220.66f),
            new Vector2(181.56f, -221.96f),
            new Vector2(216.11f, -221.99f),
            new Vector2(202.37f, -222.03f),
            new Vector2(210.46f, -222.38f),
            new Vector2(220.54f, -221.82f),
            new Vector2(179f, -222.76f),
            new Vector2(190.11f, -222.3f),
            new Vector2(200.3f, -222.08f),
            new Vector2(225.3f, -222.65f),
            new Vector2(214.11f, -223.5f),
            new Vector2(197.78f, -223.13f)
        };

        // --- ĐẶT CÁC NHÂN VẬT DÂN LÀNG NPC VÀO THỊ TRẤN PELICAN ---
        private static void BuildTownNPCs(Transform parent, Vector2 center)
        {
            // 1. CỤ BÁ (Trưởng Làng) - Đứng trước Biệt Thự Trưởng Làng hoặc tại Quảng Trường
            Vector2 elderPos = new Vector2(center.x - 2.9f, center.y - 15.2f);
            CreateNPC(parent, "C\u1EE5 B\u00E1", "Tr\u01B0\u1EDFng L\u00E0ng Pelican", elderPos, "elder",
                new string[] {
                    "Ch\u00E0o m\u1EEBng Khoa \u0111\u1EBFn v\u1EDBi Th\u1ECB tr\u1EA5n Pelican! Ti\u1EBFng t\u0103m v\u1EC1 tr\u1EA1i heo c\u1EE7a ch\u00E1u vang d\u1ED9i kh\u1EAFp thung l\u0169ng.",
                    "L\u00E0ng ta c\u00F3 ti\u1EC7m b\u00E1ch h\u00F3a c\u00F4 Mai, l\u00F2 r\u00E8n b\u00E1c H\u00F9ng v\u00E0 qu\u00E1n tr\u00E0 b\u00E9 Linh b\u00EAn b\u1EDD s\u00F4ng, ch\u00E1u c\u1EE9 thong th\u1EA3 gh\u00E9 th\u0103m nh\u00E9!",
                    "Ch\u00E1u c\u00F3 th\u1EC3 \u0111i d\u1EA1o qua c\u1EA7u g\u1ED7 ng\u1EAFm d\u00F2ng s\u00F4ng Pelican l\u1EA5p l\u00E1nh v\u00E0 c\u00E1c r\u1EB7ng hoa anh \u0111\u00E0o n\u1EDF r\u1ED9."
                });

            // 2. CÔ MAI (Chủ Tiệm Bách Hóa) - Đứng trước Tiệm Bách Hóa Pierre
            Vector2 maiPos = new Vector2(center.x - 12.5f, center.y - 1.2f);
            CreateNPC(parent, "C\u00F4 Mai", "Ch\u1EE7 Ti\u1EC7m B\u00E1ch H\u00F3a", maiPos, "mai",
                new string[] {
                    "Ch\u00E0o Khoa! V\u01B0\u1EDDn hoa h\u1ED3ng Juliet v\u00E0 Linh Lan c\u1EE7a ch\u00E1u tr\u1ED3ng ngo\u00E0i trang tr\u1EA1i n\u1EDF \u0111\u1EB9p tuy\u1EC7t v\u1EDDi!",
                    "C\u1EA7n mua th\u00EAm m\u00E1ng \u0103n, x\u00F4 n\u01B0\u1EDBc hay h\u1EA1t gi\u1ED1ng m\u1EDBi c\u1EE9 n\u00F3i c\u00F4 nh\u00E9, gi\u00E1 lu\u00F4n \u01B0u \u0111\u00E3i cho Khoa!",
                    "D\u00E2n l\u00E0ng Pelican ai c\u0169ng y\u00EAu m\u1EBFn trang tr\u1EA1i v\u00E0 \u0111\u00E0n heo \u1EE7n \u1EC9 c\u1EE7a ch\u00E1u \u0111\u1EA5y!"
                });

            // 3. BÁC HÙNG (Thợ Rèn Kim Khí) - Đứng trước Lò Rèn Clint bên kia sông
            Vector2 hungPos = new Vector2(center.x + 12.9f, center.y - 13.8f);
            CreateNPC(parent, "B\u00E1c H\u00F9ng", "Th\u1EE3 R\u00E8n Kim Kh\u00ED", hungPos, "hung",
                new string[] {
                    "Keng! Keng! Ch\u00E0o Khoa! Ti\u1EC7m r\u00E8n b\u00E1c lu\u00F4n \u0111\u1ECF l\u1EEDa s\u1EB5n s\u00E0ng r\u00E8n b\u00FAa, k\u1EB7p s\u1EAFt v\u00E0 n\u00E2ng c\u1EA5p n\u00F4ng c\u1EE5 cho ch\u00E1u.",
                    "H\u00E0ng r\u00E0o g\u1ED7 chu\u1ED3ng heo ph\u1EA3i g\u00E3i then s\u1EAFt ch\u1EAFc ch\u1EAFn th\u00EC m\u1EDBi kh\u00F4ng s\u1EE3 gi\u00F3 b\u00E3o hay th\u00FA d\u1EEF nh\u00E9!",
                    "Mu\u1ED1n r\u00E8n th\u00EAm chu\u00F4ng \u0111eo c\u1ED5 heo hay b\u00F3ng \u0111\u00E8n kim lo\u1EA1i c\u1EE9 mang qu\u1EB7ng qua \u0111\u00E2y cho b\u00E1c!"
                });

            // 4. BÉ LINH (Chủ Quán Trà & Nước Uống) - Đứng ngoài Quán Rượu / Café Saloon
            Vector2 linhPos = new Vector2(center.x - 11.4f, center.y - 8.8f);
            CreateNPC(parent, "B\u00E9 Linh", "Ch\u1EE7 Qu\u00E1n Tr\u00E0 Th\u1EA3o M\u1ED9c", linhPos, "linh",
                new string[] {
                    "Anh Khoa \u01A1i! \u0110i b\u1ED9 t\u1EEB trang tr\u1EA1i qua \u0111\u00E2y c\u00F3 m\u1EC7t kh\u00F4ng? U\u1ED1ng ngay t\u00E1ch tr\u00E0 hoa c\u00FAc th\u1EA3o m\u1ED9c n\u00E0y cho h\u1ED3i 100% th\u1EC3 l\u1EF1c nh\u00E9! (\u0110\u00E3 h\u1ED3i \u0111\u1EA7y th\u1EC3 l\u1EF1c)",
                    "Tr\u00E0 n\u00E0y em h\u00E1i t\u1EEB th\u1EA3o nguy\u00EAn pha m\u1EADt ong r\u1EEBng, u\u1ED1ng v\u00E0o l\u00E0 kh\u1ECFe kho\u1EAFn l\u00E0m vi\u1EC7c c\u1EA3 ng\u00E0y kh\u00F4ng bi\u1EBFt m\u1EC7t!",
                    "Th\u1ECB tr\u1EA5n Pelican m\u00F9a hoa anh \u0111\u00E0o n\u1EDF r\u1ED9 \u0111\u1EB9p nh\u01B0 trong tranh anh nh\u1EC9!"
                });
        }

        private static void CreateNPC(Transform parent, string name, string title, Vector2 pos, string style, string[] dialogues)
        {
            var npcObj = new GameObject($"NPC_{name}");
            npcObj.transform.SetParent(parent, false);
            npcObj.transform.position = new Vector3(pos.x, pos.y, 0f);
            npcObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            var sr = npcObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetNPCSprite(style);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = npcObj.AddComponent<CircleCollider2D>();
            col.radius = 0.9f;
            col.isTrigger = true;

            var npcView = npcObj.AddComponent<VillageNPCView>();
            npcView.NPCName = name;
            npcView.Title = title;
            npcView.Dialogues = dialogues;
        }

        // --- CỔNG NAM THỊ TRẤN & BIỂN BÁO QUAY VỀ NÔNG TRẠI ---
        private static void BuildTownSouthPortal(Transform parent, Vector2 center)
        {
            // Tọa độ cầu gỗ phía Nam dẫn ra biển / về nông trại: X ~ 195.2f, Y ~ -224.25f
            Vector3 bridgePos = new Vector3(center.x - 4.8f, center.y - 24.25f, 0f);

            // Hai cột đèn đá cổ kính bên mép cầu
            CreateStoneLanternPost(parent, new Vector3(bridgePos.x - 2.0f, bridgePos.y, 0f));
            CreateStoneLanternPost(parent, new Vector3(bridgePos.x + 2.0f, bridgePos.y, 0f));

            // Biển báo quay về nông trại
            var returnSignObj = new GameObject("Signpost_Return_To_Farm");
            returnSignObj.transform.SetParent(parent, false);
            returnSignObj.transform.position = new Vector3(bridgePos.x + 2.2f, bridgePos.y + 1.2f, 0f);
            returnSignObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var retSr = returnSignObj.AddComponent<SpriteRenderer>();
            retSr.sprite = GetSignpostSprite(false);
            retSr.sortingOrder = Mathf.RoundToInt(-(bridgePos.y + 1.2f) * 100);

            var retCol = returnSignObj.AddComponent<BoxCollider2D>();
            retCol.size = new Vector2(1.2f, 1.0f);
            retCol.isTrigger = true;

            var retTrigger = returnSignObj.AddComponent<VillageTravelTrigger>();
            retTrigger.TargetDestination = VillageTravelTrigger.DestinationType.ToFarm;
            retTrigger.PromptText = "Quay v\u1EC1 N\u00F4ng Tr\u1EA1i Khoa";
        }

        private static void CreateStoneLanternPost(Transform parent, Vector3 pos)
        {
            var postObj = new GameObject("Stone_Lantern_Post");
            postObj.transform.SetParent(parent, false);
            postObj.transform.position = pos;

            var sr = postObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetLanternPostSprite();
            sr.sortingOrder = Mathf.RoundToInt(-(pos.y - 0.2f) * 100);

            var col = postObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);
            col.offset = new Vector2(0f, -0.3f);
            col.isTrigger = false;
        }

        // =========================================================================
        // HỆ THỐNG NẠP SPRITE VÀ ĐỒ HỌA PIXEL ART
        // =========================================================================

        public static Sprite GetPelicanTownMapSprite()
        {
            string key = "pelican_town_full_map";
            if (spriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            string fullPath = Path.Combine(Application.dataPath, "Art/Sprites/Environment/pelican_town_map.jpg");
            if (!File.Exists(fullPath))
            {
                // Fallback check in brain user_uploaded
                fullPath = "C:/Users/Nhat/.gemini/antigravity/brain/57cdd1a2-558b-4f02-9e16-d6f152d85f49/.user_uploaded/media_1793255017253_54bceda9.jpg";
            }

            if (File.Exists(fullPath))
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    // Chuẩn 16 pixels / unit của Stardew Valley: 1024 / 16 = 64m rộng, 866 / 16 = 54.125m cao
                    var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), MapPPU);
                    spriteCache[key] = spr;
                    return spr;
                }
            }

            return null;
        }

        public static Sprite GetDirtRoadSprite()
        {
            string key = "stardew_village_dirt_road";
            if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            int w = 32, h = 32;
            var spr = CreateSprite(w, h, (p, width, height) =>
            {
                Color32 dirtBase = new Color32(196, 134, 76, 255);
                Color32 dirtDark = new Color32(165, 110, 60, 255);
                Color32 dirtLight = new Color32(220, 158, 98, 255);
                Color32 pebble = new Color32(130, 95, 65, 255);

                for (int i = 0; i < p.Length; i++) p[i] = dirtBase;

                DrawPoint(p, w, h, 4, 11, pebble);
                DrawPoint(p, w, h, 5, 11, dirtLight);
                DrawPoint(p, w, h, 18, 5, pebble);
                DrawPoint(p, w, h, 26, 20, pebble);
                DrawPoint(p, w, h, 12, 27, dirtDark);
                DrawPoint(p, w, h, 13, 27, dirtLight);
            }, 32f);

            spr.texture.wrapMode = TextureWrapMode.Repeat;
            spriteCache[key] = spr;
            return spr;
        }

        public static Sprite GetNPCSprite(string style)
        {
            string key = $"stardew_npc_{style}";
            if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var spr = CreateSprite(32, 42, (p, w, h) =>
            {
                Color32 skin = new Color32(245, 195, 155, 255);
                Color32 hair = new Color32(75, 45, 25, 255);
                Color32 eyes = new Color32(30, 20, 15, 255);
                Color32 shirt = new Color32(50, 120, 180, 255);
                Color32 pants = new Color32(40, 55, 75, 255);

                if (style.Contains("elder"))
                {
                    hair = new Color32(225, 228, 232, 255);
                    shirt = new Color32(110, 65, 145, 255);
                    pants = new Color32(65, 45, 85, 255);
                }
                else if (style.Contains("mai"))
                {
                    hair = new Color32(125, 65, 35, 255);
                    shirt = new Color32(225, 75, 95, 255);
                    pants = new Color32(240, 235, 215, 255);
                }
                else if (style.Contains("hung"))
                {
                    hair = new Color32(45, 40, 38, 255);
                    shirt = new Color32(155, 98, 52, 255);
                    pants = new Color32(48, 44, 42, 255);
                }
                else if (style.Contains("linh"))
                {
                    hair = new Color32(68, 42, 28, 255);
                    shirt = new Color32(55, 168, 122, 255);
                    pants = new Color32(245, 240, 215, 255);
                }

                DrawRect(p, w, h, 11, 2, 4, 11, pants);
                DrawRect(p, w, h, 17, 2, 4, 11, pants);

                DrawRect(p, w, h, 10, 13, 12, 13, shirt);

                DrawRect(p, w, h, 11, 26, 10, 10, skin);

                DrawPoint(p, w, h, 13, 30, eyes);
                DrawPoint(p, w, h, 18, 30, eyes);

                DrawRect(p, w, h, 10, 34, 12, 5, hair);
                DrawRect(p, w, h, 9, 30, 2, 7, hair);
                DrawRect(p, w, h, 21, 30, 2, 7, hair);

                if (style.Contains("elder"))
                {
                    DrawRect(p, w, h, 13, 23, 6, 5, hair);
                    DrawPoint(p, w, h, 15, 22, hair);
                    DrawPoint(p, w, h, 16, 22, hair);
                }
            }, 32f, new Vector2(0.5f, 0.08f));

            spriteCache[key] = spr;
            return spr;
        }

        public static Sprite GetSignpostSprite(bool toVillage)
        {
            string key = toVillage ? "sign_to_village" : "sign_to_farm";
            if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var spr = CreateSprite(40, 48, (p, w, h) =>
            {
                Color32 woodDark = new Color32(50, 30, 16, 255);
                Color32 woodMid = new Color32(110, 70, 38, 255);
                Color32 woodLight = new Color32(165, 115, 68, 255);
                Color32 signGold = new Color32(255, 215, 60, 255);

                DrawRect(p, w, h, 18, 2, 4, 28, woodDark);
                DrawRect(p, w, h, 19, 2, 2, 28, woodMid);

                DrawRect(p, w, h, 4, 24, 32, 18, woodDark);
                DrawRect(p, w, h, 6, 26, 28, 14, woodMid);
                DrawRect(p, w, h, 8, 28, 24, 10, woodLight);

                if (toVillage)
                {
                    DrawLine(p, w, h, 12, 32, 20, 36, signGold, 2);
                    DrawLine(p, w, h, 20, 36, 28, 32, signGold, 2);
                    DrawRect(p, w, h, 15, 29, 10, 4, signGold);
                }
                else
                {
                    DrawLine(p, w, h, 12, 33, 20, 33, signGold, 2);
                    DrawLine(p, w, h, 16, 30, 12, 33, signGold, 2);
                    DrawLine(p, w, h, 16, 36, 12, 33, signGold, 2);
                }
            }, 32f, new Vector2(0.5f, 0.1f));

            spriteCache[key] = spr;
            return spr;
        }

        public static Sprite GetLanternPostSprite()
        {
            string key = "stone_lantern_post";
            if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var spr = CreateSprite(24, 48, (p, w, h) =>
            {
                Color32 stoneDark = new Color32(55, 55, 60, 255);
                Color32 stoneMid = new Color32(105, 105, 110, 255);
                Color32 stoneLight = new Color32(155, 155, 160, 255);
                Color32 lanternGlow = new Color32(255, 210, 60, 255);
                Color32 flame = new Color32(255, 245, 180, 255);

                DrawRect(p, w, h, 6, 2, 12, 6, stoneDark);
                DrawRect(p, w, h, 8, 4, 8, 4, stoneMid);

                DrawRect(p, w, h, 9, 8, 6, 22, stoneDark);
                DrawRect(p, w, h, 10, 8, 4, 22, stoneMid);
                DrawRect(p, w, h, 11, 8, 2, 22, stoneLight);

                DrawRect(p, w, h, 5, 30, 14, 4, stoneDark);
                DrawRect(p, w, h, 7, 34, 10, 8, lanternGlow);
                DrawPoint(p, w, h, 11, 38, flame);
                DrawPoint(p, w, h, 12, 38, flame);

                DrawRect(p, w, h, 4, 42, 16, 3, stoneDark);
                DrawRect(p, w, h, 7, 45, 10, 2, stoneMid);
            }, 32f, new Vector2(0.5f, 0.1f));

            spriteCache[key] = spr;
            return spr;
        }

        private static Sprite CreateSprite(int w, int h, Action<Color32[], int, int> draw, float ppu = 32f, Vector2? pivot = null)
        {
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            draw(pixels, w, h);

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Vector2 piv = pivot ?? new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, new Rect(0, 0, w, h), piv, ppu);
        }

        private static void DrawPoint(Color32[] p, int w, int h, int x, int y, Color32 color)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                p[y * w + x] = BlendColor(p[y * w + x], color);
            }
        }

        private static void DrawRect(Color32[] p, int w, int h, int rx, int ry, int rw, int rh, Color32 color)
        {
            int x0 = Mathf.Clamp(rx, 0, w);
            int y0 = Mathf.Clamp(ry, 0, h);
            int x1 = Mathf.Clamp(rx + rw, 0, w);
            int y1 = Mathf.Clamp(ry + rh, 0, h);

            for (int y = y0; y < y1; y++)
            {
                int row = y * w;
                for (int x = x0; x < x1; x++)
                {
                    p[row + x] = BlendColor(p[row + x], color);
                }
            }
        }

        private static void DrawLine(Color32[] p, int w, int h, int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (thickness <= 1)
                {
                    DrawPoint(p, w, h, x0, y0, color);
                }
                else
                {
                    int half = thickness / 2;
                    DrawRect(p, w, h, x0 - half, y0 - half, thickness, thickness, color);
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static Color32 BlendColor(Color32 dst, Color32 src)
        {
            if (src.a == 255) return src;
            if (src.a == 0) return dst;
            float a = src.a / 255f;
            float inv = 1f - a;
            byte r = (byte)(src.r * a + dst.r * inv);
            byte g = (byte)(src.g * a + dst.g * inv);
            byte b = (byte)(src.b * a + dst.b * inv);
            byte finalA = (byte)Mathf.Clamp(dst.a + src.a, 0, 255);
            return new Color32(r, g, b, finalA);
        }
    }

    /// <summary>
    /// Hệ thống hiệu ứng khí quyển và sức sống cho Thị trấn Pelican (Làng NPC):
    /// 1. Cánh hoa anh đào (Sakura Petals) rơi lướt nhẹ nhàng trong gió xuân.
    /// 2. Khói lam chiều bốc lên từ các ống khói (Tiệm rèn, Tiệm bách hóa, Quán rượu, Tòa thị chính).
    /// 3. Lò rèn Bác Hùng đỏ lửa bập bùng (Forge Fire Flickering Light).
    /// 4. Đèn đường cổ kính và cửa sổ phát sáng ấm áp khi hoàng hôn/đêm xuống.
    /// 5. Bọt nước và tia nước đài phun nước trung tâm lấp lánh.
    /// 6. Bọt sóng lăn tăn ở chân thác nước bờ sông.
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCVillageAtmosphere : MonoBehaviour
    {
        private static Sprite petalSprite;
        private static Sprite smokeSprite;
        private static Sprite dropletSprite;
        private static Sprite chatBubbleSprite;

        private struct Petal
        {
            public GameObject obj;
            public SpriteRenderer sr;
            public Vector3 pos;
            public float speedX;
            public float speedY;
            public float swayFrequency;
            public float swayAmplitude;
            public float swayOffset;
            public float rotationSpeed;
        }

        private struct SmokePuff
        {
            public GameObject obj;
            public SpriteRenderer sr;
            public Vector3 startPos;
            public Vector3 currentPos;
            public float age;
            public float maxLifetime;
            public float startScale;
            public float endScale;
            public float driftX;
        }

        private readonly List<Petal> petals = new List<Petal>();
        private readonly List<SmokePuff> smokePuffs = new List<SmokePuff>();
        private readonly List<Light2D> villageLights = new List<Light2D>();
        private Light2D forgeFlickerLight;

        private readonly Vector2[] chimneyPositions = new Vector2[]
        {
            new Vector2(212.9f, -208.0f), // Lò rèn Clint
            new Vector2(185.5f, -195.5f), // Tiệm bách hóa Pierre
            new Vector2(190.5f, -203.0f), // Quán rượu Saloon
            new Vector2(199.5f, -209.5f), // Biệt thự Mayor Lewis
            new Vector2(195.0f, -177.5f)  // Community Center
        };

        private float smokeSpawnTimer = 0f;
        private float waterSplashTimer = 0f;
        private Vector2 fountainPos = new Vector2(181.1f, -186.1f);
        private Vector2 waterfallPos = new Vector2(216.5f, -173.0f);

        public static void AttachTo(Transform villageRoot, Vector2 center)
        {
            if (villageRoot == null) return;
            var existing = villageRoot.GetComponent<NPCVillageAtmosphere>();
            if (existing != null) GameObject.DestroyImmediate(existing);

            var atmos = villageRoot.gameObject.AddComponent<NPCVillageAtmosphere>();
            atmos.Initialize(center);
        }

        public void Initialize(Vector2 center)
        {
            EnsureSprites();
            CreatePetals(30, center);
            CreateStreetAndWindowLights(center);
        }

        private void Update()
        {
            UpdatePetals();
            UpdateSmoke();
            UpdateLightsAndForge();
            UpdateFountainAndWaterfall();
        }

        private void CreatePetals(int count, Vector2 center)
        {
            var petalContainer = new GameObject("Atmo_Sakura_Petals");
            petalContainer.transform.SetParent(transform, false);

            for (int i = 0; i < count; i++)
            {
                var pObj = new GameObject($"Petal_{i}");
                pObj.transform.SetParent(petalContainer.transform, false);

                var sr = pObj.AddComponent<SpriteRenderer>();
                sr.sprite = petalSprite;
                sr.sortingOrder = 9500;

                float rx = UnityEngine.Random.Range(center.x - 30f, center.x + 30f);
                float ry = UnityEngine.Random.Range(center.y - 25f, center.y + 25f);
                Vector3 startPos = new Vector3(rx, ry, 0f);
                pObj.transform.position = startPos;

                petals.Add(new Petal
                {
                    obj = pObj,
                    sr = sr,
                    pos = startPos,
                    speedX = UnityEngine.Random.Range(-1.2f, -0.6f),
                    speedY = UnityEngine.Random.Range(-0.8f, -0.4f),
                    swayFrequency = UnityEngine.Random.Range(1.8f, 3.2f),
                    swayAmplitude = UnityEngine.Random.Range(0.6f, 1.2f),
                    swayOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    rotationSpeed = UnityEngine.Random.Range(-45f, 45f)
                });
            }
        }

        private void UpdatePetals()
        {
            Vector2 center = NPCVillageManager.VillageCenter;
            float minX = center.x - 34f, maxX = center.x + 34f;
            float minY = center.y - 28f, maxY = center.y + 28f;

            for (int i = 0; i < petals.Count; i++)
            {
                var p = petals[i];
                float dt = Time.deltaTime;

                float sway = Mathf.Sin(Time.time * p.swayFrequency + p.swayOffset) * p.swayAmplitude;
                p.pos.x += (p.speedX + sway * 0.4f) * dt;
                p.pos.y += p.speedY * dt;

                if (p.pos.y < minY || p.pos.x < minX)
                {
                    p.pos.x = UnityEngine.Random.Range(center.x - 10f, maxX + 5f);
                    p.pos.y = UnityEngine.Random.Range(center.y + 10f, maxY + 2f);
                }

                p.obj.transform.position = p.pos;
                p.obj.transform.Rotate(0f, 0f, p.rotationSpeed * dt);
                petals[i] = p;
            }
        }

        private void UpdateSmoke()
        {
            smokeSpawnTimer += Time.deltaTime;
            if (smokeSpawnTimer >= 0.75f)
            {
                smokeSpawnTimer = 0f;
                Vector2 spawnPos = chimneyPositions[UnityEngine.Random.Range(0, chimneyPositions.Length)];
                SpawnSmokePuff(spawnPos);
            }

            for (int i = smokePuffs.Count - 1; i >= 0; i--)
            {
                var s = smokePuffs[i];
                s.age += Time.deltaTime;
                float t = s.age / s.maxLifetime;

                if (t >= 1.0f)
                {
                    GameObject.Destroy(s.obj);
                    smokePuffs.RemoveAt(i);
                    continue;
                }

                s.currentPos.y += 0.85f * Time.deltaTime;
                s.currentPos.x += (s.driftX + Mathf.Sin(s.age * 2.5f) * 0.3f) * Time.deltaTime;
                s.obj.transform.position = s.currentPos;

                float scale = Mathf.Lerp(s.startScale, s.endScale, t);
                s.obj.transform.localScale = new Vector3(scale, scale, 1f);

                float alpha = Mathf.Sin(t * Mathf.PI) * 0.45f;
                s.sr.color = new Color(0.88f, 0.88f, 0.92f, alpha);

                smokePuffs[i] = s;
            }
        }

        private void SpawnSmokePuff(Vector2 pos)
        {
            var sObj = new GameObject("Smoke_Puff");
            sObj.transform.SetParent(transform, false);
            sObj.transform.position = new Vector3(pos.x, pos.y, 0f);

            var sr = sObj.AddComponent<SpriteRenderer>();
            sr.sprite = smokeSprite;
            sr.color = new Color(0.88f, 0.88f, 0.92f, 0f);
            sr.sortingOrder = 9000;

            smokePuffs.Add(new SmokePuff
            {
                obj = sObj,
                sr = sr,
                startPos = pos,
                currentPos = pos,
                age = 0f,
                maxLifetime = UnityEngine.Random.Range(3.2f, 4.5f),
                startScale = UnityEngine.Random.Range(0.4f, 0.6f),
                endScale = UnityEngine.Random.Range(1.2f, 1.6f),
                driftX = UnityEngine.Random.Range(-0.3f, -0.1f)
            });
        }

        private void CreateStreetAndWindowLights(Vector2 center)
        {
            var lightContainer = new GameObject("Atmo_Village_Lights");
            lightContainer.transform.SetParent(transform, false);

            var forgeLightObj = new GameObject("Light_Forge_Fire");
            forgeLightObj.transform.SetParent(lightContainer.transform, false);
            forgeLightObj.transform.position = new Vector3(center.x + 12.9f, center.y - 12.5f, 0f);

            forgeFlickerLight = forgeLightObj.AddComponent<Light2D>();
            forgeFlickerLight.lightType = Light2D.LightType.Point;
            forgeFlickerLight.pointLightInnerRadius = 0.5f;
            forgeFlickerLight.pointLightOuterRadius = 6.5f;
            forgeFlickerLight.color = new Color(1.0f, 0.52f, 0.18f, 1.0f);
            forgeFlickerLight.intensity = 0.85f;

            Vector2[] lampPositions = new Vector2[]
            {
                new Vector2(center.x - 4.8f, center.y - 24.25f),
                new Vector2(center.x + 2.5f, center.y - 20.0f),
                new Vector2(center.x - 12.5f, center.y - 5.5f),
                new Vector2(center.x - 12.5f, center.y + 0.5f),
                new Vector2(center.x + 8.5f, center.y - 0.5f),
                new Vector2(center.x + 12.9f, center.y - 9.5f),
                new Vector2(center.x - 2.9f, center.y - 14.0f),
                new Vector2(center.x - 6.6f, center.y + 16.5f)
            };

            foreach (var pos in lampPositions)
            {
                var lObj = new GameObject($"Lamp_Light_{pos.x:F0}_{pos.y:F0}");
                lObj.transform.SetParent(lightContainer.transform, false);
                lObj.transform.position = new Vector3(pos.x, pos.y, 0f);

                var l2d = lObj.AddComponent<Light2D>();
                l2d.lightType = Light2D.LightType.Point;
                l2d.pointLightInnerRadius = 0.6f;
                l2d.pointLightOuterRadius = 5.5f;
                l2d.color = new Color(1.0f, 0.85f, 0.55f, 1.0f);
                l2d.intensity = 0.65f;
                villageLights.Add(l2d);
            }
        }

        private void UpdateLightsAndForge()
        {
            if (forgeFlickerLight != null)
            {
                float flicker = Mathf.Sin(Time.time * 9f) * 0.12f + Mathf.Cos(Time.time * 15f) * 0.08f;
                forgeFlickerLight.intensity = 0.82f + flicker;
            }

            var engine = MobileGameController.Instance?.Engine;
            bool isNight = engine != null && engine.Clock != null && engine.Clock.IsNight;
            float targetIntensity = isNight ? 0.75f : 0.0f;

            foreach (var l in villageLights)
            {
                if (l != null)
                {
                    l.intensity = Mathf.MoveTowards(l.intensity, targetIntensity, Time.deltaTime * 1.5f);
                }
            }
        }

        private void UpdateFountainAndWaterfall()
        {
            waterSplashTimer += Time.deltaTime;
            if (waterSplashTimer >= 0.25f)
            {
                waterSplashTimer = 0f;
                SpawnWaterDroplet(fountainPos + new Vector2(UnityEngine.Random.Range(-0.8f, 0.8f), UnityEngine.Random.Range(0.2f, 1.2f)));
                SpawnWaterDroplet(waterfallPos + new Vector2(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-0.5f, 0.5f)));
            }
        }

        private void SpawnWaterDroplet(Vector2 pos)
        {
            var dObj = new GameObject("Water_Droplet");
            dObj.transform.SetParent(transform, false);
            dObj.transform.position = new Vector3(pos.x, pos.y, 0f);

            var sr = dObj.AddComponent<SpriteRenderer>();
            sr.sprite = dropletSprite;
            sr.color = new Color(0.7f, 0.95f, 1.0f, 0.85f);
            sr.sortingOrder = 8000;

            GameObject.Destroy(dObj, UnityEngine.Random.Range(0.4f, 0.7f));
        }

        private static void EnsureSprites()
        {
            if (petalSprite != null && smokeSprite != null && dropletSprite != null) return;

            petalSprite = CreatePixelSprite(10, 10, (p, w, h) =>
            {
                Color32 pinkDeep = new Color32(235, 115, 160, 255);
                Color32 pinkMid = new Color32(255, 175, 205, 255);
                Color32 pinkWhite = new Color32(255, 235, 245, 255);

                for (int y = 1; y < 9; y++)
                {
                    int span = (y <= 5) ? y : (9 - y);
                    for (int x = 5 - span; x <= 5 + span; x++)
                    {
                        if (x >= 0 && x < w)
                        {
                            p[y * w + x] = (x == 5 && y >= 3 && y <= 6) ? pinkWhite : (y % 2 == 0 ? pinkMid : pinkDeep);
                        }
                    }
                }
            }, 32f);

            smokeSprite = CreatePixelSprite(24, 24, (p, w, h) =>
            {
                Color32 smoke = new Color32(220, 222, 230, 255);
                float radius = 10f;
                float r2 = radius * radius;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float dx = x - 12f;
                        float dy = y - 12f;
                        float d2 = dx * dx + dy * dy;
                        if (d2 <= r2)
                        {
                            float alpha = 1f - Mathf.Sqrt(d2) / radius;
                            byte a = (byte)(alpha * 230);
                            p[y * w + x] = new Color32(smoke.r, smoke.g, smoke.b, a);
                        }
                    }
                }
            }, 24f);

            dropletSprite = CreatePixelSprite(6, 6, (p, w, h) =>
            {
                Color32 cyanWhite = new Color32(225, 250, 255, 255);
                p[2 * w + 2] = cyanWhite;
                p[2 * w + 3] = cyanWhite;
                p[3 * w + 2] = cyanWhite;
                p[3 * w + 3] = cyanWhite;
            }, 32f);

            chatBubbleSprite = CreatePixelSprite(24, 20, (p, w, h) =>
            {
                Color32 border = new Color32(45, 45, 50, 255);
                Color32 bg = new Color32(255, 255, 255, 255);
                Color32 dot = new Color32(65, 65, 75, 255);

                for (int y = 4; y < 18; y++)
                {
                    for (int x = 2; x < 22; x++)
                    {
                        p[y * w + x] = bg;
                    }
                }
                for (int x = 4; x < 20; x++) { p[18 * w + x] = border; p[4 * w + x] = border; }
                for (int y = 6; y < 17; y++) { p[y * w + 2] = border; p[y * w + 21] = border; }
                p[3 * w + 8] = border; p[3 * w + 9] = bg; p[3 * w + 10] = border;
                p[2 * w + 7] = border; p[2 * w + 8] = bg;
                p[1 * w + 6] = border;

                p[11 * w + 7] = dot; p[10 * w + 7] = dot;
                p[11 * w + 12] = dot; p[10 * w + 12] = dot;
                p[11 * w + 17] = dot; p[10 * w + 17] = dot;
            }, 24f, new Vector2(0.5f, 0f));
        }

        public static Sprite GetChatBubbleSprite()
        {
            EnsureSprites();
            return chatBubbleSprite;
        }

        private static Sprite CreatePixelSprite(int w, int h, Action<Color32[], int, int> draw, float ppu = 32f, Vector2? pivot = null)
        {
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            draw(pixels, w, h);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            Vector2 piv = pivot ?? new Vector2(0.5f, 0.5f);
            return Sprite.Create(tex, new Rect(0, 0, w, h), piv, ppu);
        }
    }
}
