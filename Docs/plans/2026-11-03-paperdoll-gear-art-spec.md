# Paper-doll gear art spec (Claude → Argon), 3 Nov 2026

**User ruling (3 Nov 2026):** gear is many separate pieces fitted into the 6 slots. Pig Tycoon has **no armour sets**.

The "Bộ II Khinh Giáp / Bộ III Huyết Long / Bộ IV Tinh Vân" outfits are rejected, as are the invented weapons "Thần Kiếm Hàn Băng / Song Thương Hoàng Kim / Chiến Chùy Lôi Điện". The set swap code `HeroVisualAppearanceSync` has been removed.

## How it works

Logic is in `Core/EquipmentAppearance.cs` and rendering in `Controllers/CharacterGearLayers.cs`.

1. **Body sheet.** The existing `an` / `khoa` family shows the protagonist in plain everyday clothes, **with nothing in the hands, no armour and no weapon**. The bodies need to be re-exported without the baked-in sword: today's sword is the cause of the "two blades" bug.
2. **One overlay sheet per equipped piece**, drawn on top of the body and synced to it frame by frame:
   - same clip, same frame index, same facing;
   - an empty slot draws nothing;
   - changing one slot changes only that layer;
   - a piece with no art yet simply doesn't show.
3. **Draw order** (Core `EquipmentAppearance.Order`), bottom to top: Pants → Boots → BodyArmor → Necklace → Bracelet → Weapon.
   - Facing up: the weapon and bracelet go behind the body, and the necklace is hidden.
   - Facing left: the weapon is behind the body, because it is held in the right hand.

## File layout

| Item | Rule |
|---|---|
| Folder | `Assets/Art/PixelMyth/Characters/<family>/` |
| Family | `an__<itemId>` / `khoa__<itemId>` (two underscores), e.g. `an__wpn_dao_rua`, `khoa__armor_ao_mua_dau` |
| Sheet | `<family>_<clip>.png` + `<family>_<clip>.json` |
| Format | Exactly like the body: cell 72×84, rows down/left/up/right, same `frames_per_row`, `fps` and pivot as the body clip of the same name |
| Pixels | Transparent everywhere except the piece |
| Clips | idle, walk, run, attack first; then interact, whistle, hit, death |
| Quality (optional) | `an__wpn_dao_rua__III`. Same piece with better material or trim (steel instead of iron, a brass rim, a cleaner edge), **not a different outfit**. If missing, the quality-I sheet is used. |
| Full list | `EquipmentAppearance.AllFamilies()`: 16 pieces × 2 protagonists |

## The 16 pieces

Names exactly as in GDD 12.3. The direction is modern rural with a mythic East–West touch, light and compact (user direction 2 Nov: "nhân vật hiện đại, vũ khí thiên nhẹ gọn"). No golden crowns, wings, cosmic cloth or crystal plates.

| Slot | id | Piece | Look |
|---|---|---|---|
| Vũ khí | wpn_dao_rua | Dao Rựa | Farm machete, worn wooden handle |
| Vũ khí | wpn_giao_moc | Giáo Móc | Short spear with a hook at the tip, bamboo or wooden shaft |
| Vũ khí | wpn_bua_ta | Búa Tạ | Heavy sledgehammer, iron head, long handle |
| Vũ khí | wpn_no_tay | Nỏ Tay | Compact hand crossbow, small quiver at the waist |
| Giày | boot_giay_dinh | Giày Đinh | Work boots with spiked soles |
| Giày | boot_guoc_gio | Guốc Gió | Light wooden clogs with cloth straps |
| Giáp thân | armor_giap_da_thuoc | Giáp Da Thuộc | Brown tanned-leather vest |
| Giáp thân | armor_giap_luoi_thep | Giáp Lưới Thép | Steel-mesh vest over a shirt, heavy |
| Giáp thân | armor_ao_mua_dau | Áo Mưa Dầu | Oiled raincoat, dark yellow or olive |
| Giáp thân | armor_ao_chi | Áo Chì | Dark-grey lead apron, heavy |
| Quần giáp | pants_quan_da_lot_bong | Quần Da Lót Bông | Leather trousers padded with cotton |
| Quần giáp | pants_quan_giap_xich | Quần Giáp Xích | Chain-mail trousers |
| Vòng tay | brc_vong_cach_dien | Vòng Cách Điện | Black rubber insulating band |
| Vòng tay | brc_vong_huyet_nha | Vòng Huyết Nha | Bracelet of beast fangs |
| Vòng cổ | nck_coi_chan_heo | Còi Chăn Heo | Herding whistle on a cord |
| Vòng cổ | nck_mat_day_den_bao | Mặt Dây Đèn Bão | Small storm-lantern pendant |

## Priority

1. Khoa's body without a weapon. Khoa is approved.
2. The 4 weapons for Khoa.
3. The 4 body armours for Khoa.
4. Boots and pants, then bracelets and necklaces.
5. An, only after the user approves An's design here.

Before reporting done, check every sheet as a filmstrip at ×1.5 nearest-neighbour: the piece sits on the body in every frame, and the legs actually step.

## Paper-doll in the C panel (UI)

The C panel shows the same layering: body plus one layer per slot. It must not use one big picture per outfit.

Each slot in the panel shows that piece's icon, its quality frame (I–IV) and its real bonus stat. Read these from the Core item; do not invent them.
