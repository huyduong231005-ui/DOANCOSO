using t.Models.Entities;

namespace t.Data;

internal static class SampleListings
{
    private static readonly DateOnly SeedAvailableFrom = new(2026, 6, 1);

    private record Sample(
        string Cat, int ImgIdx, string Title, string Slug, string Code,
        string Desc, string DescExtra, decimal Price, decimal Deposit, string FeeNote,
        double Area, int Beds, int Baths,
        string Address, double Lat, double Lon,
        string RegionKey, string[] Amenities, bool Featured = false);

    private static readonly Sample[] Data = new Sample[]
    {
        // ── LUXURY (Căn hộ chung cư cao cấp) — giá 12-25tr, 50-100m² ──
        new("luxury", 1, "Căn hộ The Marq 2PN nội thất nhập",            "the-marq-2pn-noi-that-nhap",      "MQ-1201",
            "Căn hộ 2 phòng ngủ tại The Marq quận 1, nội thất nhập khẩu, view trung tâm.",
            "Tiện ích nội khu hồ bơi vô cực, gym, sky lounge tầng 30.",
            22_500_000m, 45_000_000m, "Đã bao gồm phí quản lý",
            78, 2, 2, "Tôn Đức Thắng, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
            10.7794, 106.7050, "hcm", new[]{"wifi","ac","pool","gym","security","furniture"}, true),

        new("luxury", 2, "Căn hộ Vinhomes Central Park 1PN view sông",   "vhcp-1pn-view-song",              "VHCP-A1505",
            "Căn 1 phòng ngủ Vinhomes Central Park, view trực diện sông Sài Gòn, full nội thất.",
            "Khu compound khép kín, 24/7 an ninh, BBQ area, công viên 14 ha.",
            18_000_000m, 36_000_000m, "Phí quản lý 16.500đ/m² đã tính riêng",
            55, 1, 1, "Nguyễn Hữu Cảnh, Phường 22, Bình Thạnh, TP. Hồ Chí Minh",
            10.7906, 106.7228, "hcm", new[]{"wifi","ac","pool","gym","security","furniture","parking"}),

        new("luxury", 3, "Sunwah Pearl 3PN tầng cao",                    "sunwah-pearl-3pn-tang-cao",       "SWP-2402",
            "Căn 3 phòng ngủ tầng 24 toà White Pearl, ánh sáng tự nhiên, ban công rộng.",
            "Bàn giao chìa khoá ngay, có thang máy riêng cho cư dân.",
            25_000_000m, 50_000_000m, "Phí gửi xe 1.5tr/tháng",
            96, 3, 2, "Nguyễn Hữu Cảnh, Phường 22, Bình Thạnh, TP. Hồ Chí Minh",
            10.7912, 106.7224, "hcm", new[]{"wifi","ac","pool","gym","security","furniture","washer"}, true),

        new("luxury", 4, "Vinhomes D'Capitale 2PN Trần Duy Hưng",        "vh-dcapitale-2pn-tran-duy-hung",  "DCP-1808",
            "Căn 2 phòng ngủ Vinhomes D'Capitale, ngay mặt đường Trần Duy Hưng, gần TTTM Big C.",
            "Toà C7, view nội khu, đầy đủ nội thất Châu Âu.",
            16_500_000m, 33_000_000m, "Đã bao điện nước cơ bản 2 người",
            72, 2, 2, "Trần Duy Hưng, Phường Trung Hoà, Cầu Giấy, Hà Nội",
            21.0091, 105.7970, "hn", new[]{"wifi","ac","gym","security","furniture","fridge"}),

        new("luxury", 5, "Indochina Plaza Hà Nội 1PN trung tâm",         "ipc-1pn-cau-giay",                "IPC-1102",
            "Căn 1 phòng ngủ Indochina Plaza Cầu Giấy, view nội khu, gần Đại học Quốc gia.",
            "Cao ốc văn phòng kết hợp căn hộ cao cấp, lễ tân lobby 24/7.",
            14_000_000m, 28_000_000m, "Bao gồm internet và truyền hình cáp",
            58, 1, 1, "241 Xuân Thuỷ, Phường Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
            21.0379, 105.7826, "hn", new[]{"wifi","ac","security","furniture","gym"}),

        // ── MINI (Chung cư mini) — giá 4-8tr, 20-35m² ──
        new("mini", 1, "Chung cư mini Tô Hiệu studio mới xây",            "ccm-to-hieu-studio",              "TH-201",
            "Studio 25m² mới xây 2024, ban công riêng, đầy đủ nội thất hiện đại.",
            "Toà 7 tầng, thang máy, hệ thống PCCC đầy đủ, sổ hồng riêng từng căn.",
            5_500_000m, 11_000_000m, "Điện 4.000đ/kWh, nước 25.000đ/m³",
            25, 1, 1, "Tô Hiệu, Phường Nghĩa Đô, Cầu Giấy, Hà Nội",
            21.0419, 105.7882, "hn", new[]{"wifi","ac","furniture","security"}),

        new("mini", 2, "Chung cư mini Cầu Giấy ban công rộng",           "ccm-cau-giay-ban-cong",           "TR-301",
            "Căn 30m² ban công thoáng, gác xép gỗ, cửa sổ to đón nắng.",
            "Khu dân trí cao, gần ĐH Sư phạm, ĐH Ngoại ngữ.",
            6_500_000m, 13_000_000m, "Mạng cáp quang miễn phí",
            30, 1, 1, "Trần Quốc Hoàn, Phường Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
            21.0367, 105.7872, "hn", new[]{"wifi","ac","fridge","furniture","washer"}),

        new("mini", 3, "Mini Tân Bình full đồ chỉ việc xách vali",       "ccm-tan-binh-full-do",            "TB-505",
            "Căn 22m² đầy đủ máy lạnh, máy giặt, tủ lạnh, bếp từ. Ở được ngay.",
            "Toà 6 tầng có thang máy, free wifi tốc độ cao.",
            4_800_000m, 9_600_000m, "Điện 3.500đ/kWh, nước 20.000đ/m³",
            22, 1, 1, "Hoàng Hoa Thám, Phường 12, Tân Bình, TP. Hồ Chí Minh",
            10.7984, 106.6444, "hcm", new[]{"wifi","ac","fridge","washer","furniture"}),

        new("mini", 4, "Mini Phú Nhuận tầng cao yên tĩnh",               "ccm-phu-nhuan-tang-cao",          "PN-602",
            "Studio 28m² tầng 6, view thoáng, hướng đón gió. Khu vực an ninh.",
            "Có chỗ để xe máy, xe đạp riêng, camera hành lang 24/7.",
            5_200_000m, 10_400_000m, "Phí dịch vụ 200k/tháng",
            28, 1, 1, "Phan Đăng Lưu, Phường 5, Phú Nhuận, TP. Hồ Chí Minh",
            10.8011, 106.6831, "hcm", new[]{"wifi","ac","security","furniture","parking"}),

        new("mini", 5, "Studio mini Đà Nẵng gần biển",                   "ccm-da-nang-gan-bien",            "DN-401",
            "Studio 26m² Sơn Trà, đi bộ 3 phút ra biển Mỹ Khê, ban công view biển.",
            "Toà 5 tầng cho thuê dài hạn, không nhận khách du lịch.",
            4_200_000m, 8_400_000m, "Bao tiền nước, điện theo đồng hồ",
            26, 1, 1, "Võ Nguyên Giáp, Phường Phước Mỹ, Sơn Trà, Đà Nẵng",
            16.0729, 108.2453, "dn", new[]{"wifi","ac","furniture"}, true),

        // ── HOUSE (Nhà nguyên căn) — giá 8-20tr, 60-150m² ──
        new("house", 1, "Nhà phố 1 trệt 2 lầu Gò Vấp 4PN",                "nha-pho-go-vap-4pn",              "GV-N1",
            "Nhà nguyên căn 1 trệt 2 lầu, 4 phòng ngủ, sân để xe ô tô. Hẻm xe hơi.",
            "Khu dân cư hiện hữu, an ninh tốt, gần chợ Hạnh Thông Tây.",
            18_000_000m, 36_000_000m, "Chủ nhà ở tầng riêng, không chung lối",
            120, 4, 3, "Quang Trung, Phường 14, Gò Vấp, TP. Hồ Chí Minh",
            10.8425, 106.6700, "hcm", new[]{"parking","security","furniture","fridge","washer"}),

        new("house", 2, "Nhà 3 tầng Thảo Điền có sân vườn",              "nha-3-tang-thao-dien",            "TD-N2",
            "Nhà nguyên căn 3 tầng + sân thượng, có sân vườn 30m², phù hợp gia đình.",
            "Khu Thảo Điền yên tĩnh, gần trường quốc tế, đi quận 1 chỉ 10 phút.",
            22_000_000m, 44_000_000m, "Hợp đồng tối thiểu 1 năm",
            150, 3, 3, "Thảo Điền, Phường Thảo Điền, TP. Thủ Đức, TP. Hồ Chí Minh",
            10.8042, 106.7361, "hcm", new[]{"parking","ac","wifi","furniture","fridge","washer","security"}, true),

        new("house", 3, "Nhà liền kề 4PN Hà Đông gần Aeon",              "nha-lien-ke-ha-dong",             "HD-N3",
            "Nhà liền kề khu đô thị, 4 phòng ngủ, có gara để 1 xe ô tô + 2 xe máy.",
            "Cách Aeon Mall Hà Đông 5 phút, công viên nội khu rộng.",
            14_500_000m, 29_000_000m, "Phí dịch vụ KĐT 1.2tr/tháng",
            96, 4, 3, "Tố Hữu, Phường Vạn Phúc, Hà Đông, Hà Nội",
            20.9788, 105.7793, "hn", new[]{"parking","security","furniture","ac"}),

        new("house", 4, "Nhà phố Tây Hồ 3PN view Hồ Tây",                "nha-pho-tay-ho-3pn",              "TH-N4",
            "Nhà 3 tầng tại khu Tây Hồ, 3 phòng ngủ rộng, tầng tum view Hồ Tây.",
            "Phù hợp gia đình hoặc người nước ngoài, gần Lotte Tây Hồ.",
            16_000_000m, 32_000_000m, "Có chỗ đỗ xe ô tô riêng",
            90, 3, 3, "Tô Ngọc Vân, Phường Quảng An, Tây Hồ, Hà Nội",
            21.0680, 105.8217, "hn", new[]{"parking","wifi","ac","furniture","fridge","security"}),

        new("house", 5, "Nhà 2 tầng Hải Châu mặt tiền kinh doanh",       "nha-hai-chau-mat-tien",           "HC-N5",
            "Nhà nguyên căn 2 tầng mặt tiền 5m, vừa ở vừa mở shop, kinh doanh tốt.",
            "Trung tâm Đà Nẵng, gần cầu Sông Hàn, an ninh khu phố tốt.",
            12_000_000m, 24_000_000m, "Tầng 1 có thể làm shop",
            85, 3, 2, "Trần Phú, Phường Hải Châu 1, Hải Châu, Đà Nẵng",
            16.0676, 108.2231, "dn", new[]{"parking","security","wifi"}),

        // ── VILLA (Biệt thự) — giá 25-60tr, 150-400m² ──
        new("villa", 1, "Biệt thự Vinhomes Riverside Long Biên",         "bt-vh-riverside-long-bien",       "VHRS-V1",
            "Biệt thự đơn lập Vinhomes Riverside, 4 phòng ngủ, có hồ bơi riêng.",
            "Compound an ninh 24/7, sân vườn 80m², trường học quốc tế trong khu.",
            55_000_000m, 110_000_000m, "Phí quản lý 18.000đ/m² đã trừ riêng",
            350, 4, 4, "Khu đô thị Vinhomes Riverside, Long Biên, Hà Nội",
            21.0518, 105.9163, "hn", new[]{"parking","pool","gym","security","wifi","ac","furniture","washer","fridge"}, true),

        new("villa", 2, "Villa Phú Mỹ Hưng 5PN có hồ bơi",                "villa-pmh-5pn-ho-boi",            "PMH-V2",
            "Villa 5 phòng ngủ Phú Mỹ Hưng, có hồ bơi và sân vườn riêng, hồ cá Koi.",
            "Khu dân cư cao cấp, đa văn hoá, an ninh tốt nhất TP.HCM.",
            58_000_000m, 116_000_000m, "Hợp đồng tối thiểu 2 năm",
            380, 5, 5, "Tôn Dật Tiên, Phường Tân Phong, Quận 7, TP. Hồ Chí Minh",
            10.7280, 106.7173, "hcm", new[]{"parking","pool","security","wifi","ac","furniture","washer","fridge","gym"}),

        new("villa", 3, "Biệt thự Lakeview City 4PN",                    "bt-lakeview-4pn",                 "LV-V3",
            "Biệt thự song lập Lakeview City An Phú, 4 phòng ngủ, sân vườn nhỏ.",
            "Khu compound, gần Metro Bến Thành - Suối Tiên (đang vận hành).",
            32_000_000m, 64_000_000m, "Phí quản lý KĐT 1.5tr/tháng",
            220, 4, 4, "Đường D52, Phường An Phú, TP. Thủ Đức, TP. Hồ Chí Minh",
            10.8005, 106.7480, "hcm", new[]{"parking","security","wifi","ac","furniture","pool"}),

        new("villa", 4, "Villa Sơn Trà view biển 3PN",                   "villa-son-tra-view-bien",         "ST-V4",
            "Villa hướng biển Sơn Trà, 3 phòng ngủ master, sân thượng BBQ.",
            "Khu nghỉ dưỡng yên tĩnh, đi bộ ra biển 5 phút.",
            28_000_000m, 56_000_000m, "Có thợ làm vườn theo tuần",
            180, 3, 3, "Hoàng Sa, Phường Thọ Quang, Sơn Trà, Đà Nẵng",
            16.1033, 108.2722, "dn", new[]{"parking","wifi","ac","furniture","fridge","washer"}, true),

        new("villa", 5, "Biệt thự Ecopark 4PN sân vườn",                 "bt-ecopark-4pn",                  "EP-V5",
            "Biệt thự đơn lập Ecopark Văn Giang, 4 phòng ngủ, sân vườn 100m².",
            "Khu xanh, hồ điều hoà, trường học và bệnh viện đầy đủ trong KĐT.",
            38_000_000m, 76_000_000m, "Phí quản lý đã bao gồm",
            260, 4, 4, "Khu đô thị Ecopark, Văn Giang, Hưng Yên",
            20.9495, 105.9417, "hn", new[]{"parking","pool","gym","security","wifi","ac","furniture"}),

        // ── PENTHOUSE — giá 30-50tr, 100-200m² ──
        new("penthouse", 1, "Penthouse Saigon Pearl 3PN sân vườn trên không", "ph-saigon-pearl-3pn",        "SP-PH1",
            "Penthouse 2 tầng Saigon Pearl Topaz, 3 phòng ngủ, sân vườn trên không 80m².",
            "Toà tháp đôi, view trực diện sông Sài Gòn, thang máy riêng.",
            48_000_000m, 96_000_000m, "Đã bao phí quản lý 6 tháng đầu",
            180, 3, 3, "Nguyễn Hữu Cảnh, Phường 22, Bình Thạnh, TP. Hồ Chí Minh",
            10.7935, 106.7195, "hcm", new[]{"parking","pool","gym","security","wifi","ac","furniture","washer"}, true),

        new("penthouse", 2, "Penthouse Vinhomes Skylake 4PN tầng cao",        "ph-vh-skylake-4pn",          "VHSL-PH2",
            "Penthouse Vinhomes Skylake Phạm Hùng, 4 phòng ngủ, view 360 độ.",
            "Tầng cao nhất toà S2, sân vườn riêng, bể sục thư giãn.",
            52_000_000m, 104_000_000m, "Phí quản lý đã tính riêng",
            195, 4, 3, "Phạm Hùng, Phường Mỹ Đình 1, Nam Từ Liêm, Hà Nội",
            21.0285, 105.7795, "hn", new[]{"parking","pool","gym","security","wifi","ac","furniture","washer","fridge"}),

        new("penthouse", 3, "Penthouse The Vista An Phú 2PN",                "ph-the-vista-2pn",           "VIS-PH3",
            "Penthouse 2 phòng ngủ The Vista An Phú, sân thượng riêng có jacuzzi.",
            "Khu căn hộ 5 sao, cách quận 1 chỉ 8 phút bằng cao tốc Sài Gòn.",
            36_000_000m, 72_000_000m, "Bao phí gửi xe 2 ô tô",
            145, 2, 3, "Xa lộ Hà Nội, Phường An Phú, TP. Thủ Đức, TP. Hồ Chí Minh",
            10.8025, 106.7460, "hcm", new[]{"parking","pool","gym","wifi","ac","furniture","security"}, true),

        new("penthouse", 4, "Penthouse Indochina Riverside Đà Nẵng",          "ph-indochina-riverside",      "ICR-PH4",
            "Penthouse view sông Hàn, 3 phòng ngủ, sân thượng có view cầu Rồng.",
            "Vị trí trung tâm Hải Châu, gần phố ẩm thực và bãi biển.",
            32_000_000m, 64_000_000m, "Quản lý nội khu chuyên nghiệp",
            155, 3, 3, "Bạch Đằng, Phường Hải Châu 1, Hải Châu, Đà Nẵng",
            16.0742, 108.2240, "dn", new[]{"parking","pool","gym","wifi","ac","furniture"}),

        new("penthouse", 5, "Penthouse Times City 2PN tầng 30",               "ph-times-city-2pn",           "TC-PH5",
            "Penthouse Times City Park Hill, tầng 30, 2 phòng ngủ thiết kế Châu Âu.",
            "Khu compound khép kín, công viên nước Vinpearl ngay nội khu.",
            30_000_000m, 60_000_000m, "Bao phí gym và bể bơi nội khu",
            120, 2, 2, "Minh Khai, Phường Vĩnh Tuy, Hai Bà Trưng, Hà Nội",
            20.9985, 105.8666, "hn", new[]{"parking","pool","gym","security","wifi","ac","furniture"}),

        // ── ROOM (Nhà trọ) — giá 2-5tr, 15-25m² ──
        new("room", 1, "Phòng trọ Bình Thạnh có gác giá tốt",            "phong-tro-binh-thanh-gac",        "BT-R1",
            "Phòng trọ 18m² có gác lửng, cửa sổ thoáng. WC riêng, không chung chủ.",
            "Khu trọ sạch sẽ, an ninh tốt, có camera hành lang.",
            2_800_000m, 5_600_000m, "Điện 3.500đ, nước 25.000đ/m³",
            18, 1, 1, "Đinh Bộ Lĩnh, Phường 26, Bình Thạnh, TP. Hồ Chí Minh",
            10.8042, 106.7104, "hcm", new[]{"wifi","ac","parking"}),

        new("room", 2, "Phòng trọ Quận 7 mới xây 2024",                  "phong-tro-quan-7-moi-xay",        "Q7-R2",
            "Phòng 22m² mới xây, máy lạnh inverter, nệm và bàn ghế đầy đủ.",
            "Gần ĐH RMIT, đường lớn xe taxi vào tận cửa.",
            3_500_000m, 7_000_000m, "Điện 4.000đ, nước 20.000đ/m³",
            22, 1, 1, "Nguyễn Văn Linh, Phường Tân Phong, Quận 7, TP. Hồ Chí Minh",
            10.7290, 106.7202, "hcm", new[]{"wifi","ac","fridge","furniture"}),

        new("room", 3, "Phòng trọ Đống Đa khu sinh viên",                "phong-tro-dong-da-sv",            "DD-R3",
            "Phòng 16m² có gác xép, đầy đủ giường tủ. Khu sinh viên đông vui.",
            "Gần ĐH Bách Khoa, ĐH Kinh tế Quốc dân.",
            2_500_000m, 5_000_000m, "Wifi miễn phí",
            16, 1, 1, "Tạ Quang Bửu, Phường Bách Khoa, Hai Bà Trưng, Hà Nội",
            21.0036, 105.8419, "hn", new[]{"wifi","furniture"}),

        new("room", 4, "Phòng trọ Cầu Giấy ban công có máy giặt",        "phong-tro-cau-giay-ban-cong",     "CG-R4",
            "Phòng 20m² có ban công riêng, sử dụng chung máy giặt khu vực, free wifi.",
            "Gần Big C Trần Duy Hưng, đầy đủ tiện nghi sinh hoạt.",
            3_200_000m, 6_400_000m, "Điện 3.800đ, nước 18.000đ/m³",
            20, 1, 1, "Trung Kính, Phường Yên Hoà, Cầu Giấy, Hà Nội",
            21.0181, 105.7943, "hn", new[]{"wifi","ac","washer","furniture"}),

        new("room", 5, "Phòng trọ Đà Nẵng gần biển giá rẻ",              "phong-tro-da-nang-gan-bien",      "DN-R5",
            "Phòng 18m² gần biển Mỹ Khê, có quạt + máy lạnh, đi bộ ra biển 5 phút.",
            "Phù hợp người đi làm, không nhận khách du lịch.",
            2_200_000m, 4_400_000m, "Điện nước theo đồng hồ riêng",
            18, 1, 1, "An Thượng, Phường Mỹ An, Ngũ Hành Sơn, Đà Nẵng",
            16.0413, 108.2486, "dn", new[]{"wifi","ac","parking"})
    };

    public static void Seed(
        AppDbContext ctx,
        string hostId,
        Region hcm, Region hn, Region dn,
        Dictionary<string, Category> cats,
        Dictionary<string, Amenity> amens)
    {
        var apartments = new List<Apartment>();
        foreach (var s in Data)
        {
            var region = s.RegionKey switch { "hcm" => hcm, "hn" => hn, _ => dn };
            var (minLeaseMonths, maxLeaseMonths) = LeaseRangeFor(s.Cat);
            apartments.Add(new Apartment
            {
                Title = s.Title, Slug = s.Slug, UnitCode = s.Code,
                Description = s.Desc, DescriptionExtra = s.DescExtra,
                Price = s.Price, DefaultDeposit = s.Deposit, FeeNote = s.FeeNote,
                Area = s.Area, Bedrooms = s.Beds, Bathrooms = s.Baths,
                Address = s.Address, Latitude = s.Lat, Longitude = s.Lon,
                FurnishingLevel = s.Amenities.Contains("furniture")
                    ? FurnishingLevel.FullyFurnished
                    : FurnishingLevel.None,
                AllowsPets = s.Cat is "house" or "villa",
                ParkingType = s.Amenities.Contains("parking")
                    ? s.Cat is "house" or "villa" ? ParkingType.Car : ParkingType.Motorbike
                    : ParkingType.None,
                AvailableFrom = SeedAvailableFrom,
                MinLeaseMonths = minLeaseMonths,
                MaxLeaseMonths = maxLeaseMonths,
                Status = ListingStatus.Active,
                Occupancy = ApartmentOccupancy.Available,
                IsFeatured = s.Featured,
                HostId = hostId, Region = region,
                Category = cats[s.Cat]
            });
        }
        ctx.Apartments.AddRange(apartments);
        ctx.SaveChanges();

        for (var i = 0; i < Data.Length; i++)
        {
            var s = Data[i];
            var apt = apartments[i];

            ctx.ApartmentImages.Add(new ApartmentImage
            {
                ApartmentId = apt.Id,
                Url = $"/img/listings/{s.Cat}-{s.ImgIdx}.jpg",
                IsCover = true,
                SortOrder = 0
            });

            foreach (var amenityKey in s.Amenities)
            {
                if (amens.TryGetValue(amenityKey, out var amen))
                {
                    ctx.ApartmentAmenities.Add(new ApartmentAmenity
                    {
                        ApartmentId = apt.Id,
                        AmenityId = amen.Id
                    });
                }
            }
        }
        ctx.SaveChanges();
    }

    private static (int Min, int Max) LeaseRangeFor(string category)
    {
        return category switch
        {
            "room" or "mini" => (3, 12),
            "house" or "villa" => (12, 36),
            _ => (6, 24)
        };
    }
}
