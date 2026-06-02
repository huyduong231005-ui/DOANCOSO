# Thiết kế hồ sơ nhu cầu thuê và xếp hạng căn phù hợp

## Bối cảnh

Trang `/Home/Rentals` hiện hỗ trợ lọc theo khu vực, giá, diện tích, loại hình,
tiện ích và các kiểu sắp xếp cơ bản. Người dùng cũng có thể bấm `Gần bạn` để
sắp xếp theo khoảng cách từ vị trí hiện tại. Bộ lọc đang chiếm toàn bộ chiều
ngang phía trên danh sách, trong khi dữ liệu tin đăng chưa đủ chi tiết để mô tả
điều kiện thuê thực tế.

Mục tiêu của vòng này là biến bộ lọc thành một luồng tìm nhà theo nhu cầu:
khách chưa đăng nhập vẫn lọc thử được; tài khoản đã đăng nhập có thể lưu một hồ
sơ nhu cầu đang hoạt động; hệ thống loại các căn không đạt điều kiện bắt buộc và
xếp hạng các căn còn lại theo mức độ phù hợp.

Thiết kế đã chọn là sidebar trái gọn và danh sách sản phẩm ba cột ở desktop.
Các tiêu chí nâng cao nằm trong drawer bên phải để không làm trang chính rối.

## Nguyên tắc ngôn ngữ

Toàn bộ nội dung người dùng nhìn thấy phải dùng tiếng Việt có dấu:

- nhãn form, lựa chọn, nút bấm và thông báo;
- validation;
- trạng thái;
- lý do phù hợp trên card;
- nội dung drawer nâng cao.

Tên class, enum, property và cột trong code tiếp tục dùng tiếng Anh để giữ quy
ước kỹ thuật hiện tại. Các enum phải có hàm ánh xạ nhãn tiếng Việt trong lớp
localization dùng chung.

## Mục tiêu

- Chuyển bộ lọc trang thuê sang sidebar trái gọn, giữ lưới ba cột trên desktop.
- Cho phép mọi khách truy cập lọc thử, kể cả chưa đăng nhập.
- Thêm một hồ sơ nhu cầu thuê đang hoạt động cho mỗi tài khoản đăng nhập.
- Đồng bộ thuộc tính điều kiện thuê giữa tin đăng, dữ liệu mẫu, màn sửa tin,
  bộ lọc và engine xếp hạng.
- Phân biệt tiêu chí `Bắt buộc` và `Mong muốn`.
- Thêm kiểu sắp xếp `Phù hợp nhất` mà không phá các kiểu sắp xếp hiện có.
- Hiển thị phần trăm phù hợp và tối đa ba lý do nổi bật trên card khi đang dùng
  kiểu sắp xếp `Phù hợp nhất`.

## Ngoài phạm vi

- Không gửi email, push notification hoặc thông báo khi có căn mới phù hợp.
- Không hỗ trợ nhiều hồ sơ có tên trên cùng một tài khoản.
- Không cho người dùng tự đặt trọng số số học cho từng tiêu chí.
- Không thêm mô hình thuộc tính động kiểu key-value.
- Không thay đổi luồng kiểm duyệt tin đăng hiện tại.
- Không thay thế hành động `Gần bạn`; vị trí hiện tại và vị trí ưu tiên là hai
  khái niệm khác nhau.
- Không thêm dịch vụ bản đồ hoặc dịch vụ định tuyến mới. Khoảng cách tiếp tục là
  khoảng cách đường chim bay tính bằng helper `GeoDistance`.

## Trải nghiệm người dùng

### Bố cục trang thuê

Desktop dùng hai vùng:

- sidebar trái rộng khoảng `260-280px`, sticky khi cuộn;
- vùng kết quả bên phải giữ lưới ba cột.

Tablet giảm danh sách còn hai cột. Mobile ẩn sidebar và dùng nút `Bộ lọc` để mở
drawer toàn màn hình.

Sidebar hiển thị các tiêu chí phổ biến:

- khu vực;
- khoảng giá;
- loại hình;
- diện tích;
- số phòng ngủ;
- tiện ích phổ biến;
- hành động `Gần bạn`;
- hành động `Bộ lọc nâng cao`;
- `Áp dụng`;
- `Xóa bộ lọc`;
- `Lưu hồ sơ nhu cầu`;
- `Áp dụng hồ sơ đã lưu` khi tài khoản đã có hồ sơ.

Drawer nâng cao chứa:

- tình trạng nội thất;
- chỗ đậu xe;
- cho phép thú cưng;
- ngày dự kiến vào ở;
- địa chỉ ưu tiên, autocomplete Photon và bản đồ OpenFreeMap;
- bán kính tối đa;
- khoảng tầng;
- hướng nhà;
- khoảng thời hạn thuê;
- công tắc `Bắt buộc` riêng cho từng tiêu chí.

Mặc định một tiêu chí là `Mong muốn`. Khi bật `Bắt buộc`, tiêu chí đó được dùng
để loại sản phẩm không đáp ứng trước khi chấm điểm.

### Lọc thử khi chưa đăng nhập

Khách chưa đăng nhập dùng được toàn bộ sidebar, drawer nâng cao, bản đồ địa chỉ
ưu tiên và kiểu sắp xếp `Phù hợp nhất`.

Khi bấm `Lưu hồ sơ nhu cầu`:

1. giữ trạng thái lọc hiện tại bằng query string;
2. chuyển đến đăng nhập với `returnUrl` trỏ về danh sách thuê;
3. sau khi đăng nhập, quay lại đúng trạng thái lọc;
4. hoàn tất thao tác lưu bằng một POST có anti-forgery;
5. điều hướng về URL sạch, bỏ cờ lưu tạm để tránh lặp.

Không dùng GET để ghi dữ liệu. Payload phải được validate lại trên server; không
tin tưởng query string hoặc dữ liệu do JavaScript gửi lên.

### Hồ sơ đã lưu

Mỗi tài khoản có tối đa một hồ sơ nhu cầu đang hoạt động. Không phân biệt tài
khoản chủ nhà hay người thuê.

Người dùng có thể:

- tạo hồ sơ lần đầu từ sidebar;
- cập nhật hồ sơ hiện tại;
- áp dụng hồ sơ đã lưu để điền lại bộ lọc;
- tiếp tục tinh chỉnh bộ lọc mà chưa ghi đè hồ sơ cho đến khi bấm lưu.

### Card kết quả phù hợp

Khi `sort=match_desc`, card hiển thị:

- phần trăm, ví dụ `92% phù hợp`;
- tối đa ba lý do khớp nổi bật, ví dụ `Đúng khoảng giá`,
  `Cách vị trí ưu tiên 1,2 km`, `Đầy đủ nội thất`.

Card không hiển thị bảng phân tích đầy đủ để giữ mật độ ba cột. Khi không dùng
`Phù hợp nhất`, card giữ bố cục gọn hiện tại và không bắt buộc hiển thị điểm.

## Thuộc tính chuẩn hóa của tin đăng

Mở rộng `Apartment` bằng các thuộc tính:

| Thuộc tính | Kiểu đề xuất | Quy tắc |
| --- | --- | --- |
| `FurnishingLevel` | enum | `None`, `Basic`, `FullyFurnished`. |
| `AllowsPets` | `bool` | Chủ tin phải chọn có hoặc không. |
| `ParkingType` | enum | `None`, `Motorbike`, `Car`. Mức `Car` bao hàm nhu cầu xe máy. |
| `AvailableFrom` | `DateOnly` | Ngày sớm nhất có thể vào ở. |
| `MinLeaseMonths` | `int` | Ít nhất `1`. |
| `MaxLeaseMonths` | `int` | Không nhỏ hơn `MinLeaseMonths`. |
| `HouseDirection` | nullable enum | Tám hướng chính; null nghĩa là chưa xác định. |
| `FloorNumber` | `int?` | Không âm; dùng cho tin chưa gắn `FloorId`. |

`FloorId` hiện có tiếp tục là nguồn chính khi căn thuộc một tầng đã quản lý.
Khi cần đối chiếu tầng, ưu tiên `Floor.Number`, sau đó mới dùng `FloorNumber`.

Các enum mới phải có nhãn tiếng Việt:

- nội thất: `Chưa có nội thất`, `Nội thất cơ bản`, `Đầy đủ nội thất`;
- chỗ đậu xe: `Không có`, `Xe máy`, `Ô tô`;
- hướng nhà: `Đông`, `Tây`, `Nam`, `Bắc`, `Đông Bắc`, `Đông Nam`,
  `Tây Bắc`, `Tây Nam`, cùng lựa chọn UI `Không quan trọng` hoặc
  `Chưa xác định` khi phù hợp ngữ cảnh.

### Đồng bộ form quản lý tin

Nhóm `Điều kiện thuê` phải xuất hiện trong:

- form đăng tin mới `/Home/PostListing`;
- form sửa tin của chủ nhà `/MyListings/Edit`;
- form sửa căn hộ của quản trị viên `/Admin/Apartments/Edit`.

Các trường bắt buộc khi tạo tin mới:

- tình trạng nội thất;
- cho phép thú cưng;
- loại chỗ đậu xe;
- ngày có thể vào ở;
- thời hạn thuê tối thiểu;
- thời hạn thuê tối đa.

Các trường không bắt buộc:

- tầng;
- hướng nhà.

Màn sửa tin và handler server phải validate lại enum, ngày, tầng và khoảng thời
hạn thuê. Không chỉ dựa vào dropdown phía client.

## Mô hình hồ sơ nhu cầu thuê

Thêm aggregate `RentalPreferenceProfile`, quan hệ một-một với `AppUser`.
Profile lưu các giá trị nullable để phân biệt rõ tiêu chí chưa chọn:

| Nhóm | Thuộc tính |
| --- | --- |
| Cơ bản | `RegionId`, `MinPrice`, `MaxPrice`, `MinArea`, `MaxArea`, `MinBedrooms`. |
| Vị trí | `PreferredAddress`, `PreferredLatitude`, `PreferredLongitude`, `MaxDistanceKm`. |
| Điều kiện thuê | `FurnishingLevel`, `AllowsPets`, `ParkingType`, `MoveInDate`, `MinLeaseMonths`, `MaxLeaseMonths`. |
| Căn hộ | `MinFloor`, `MaxFloor`, `HouseDirection`. |
| Metadata | `UserId`, audit fields. |

Thêm hai bảng liên kết:

- `RentalPreferenceCategory`: các loại hình được chấp nhận;
- `RentalPreferenceAmenity`: các tiện ích người dùng chọn, mỗi dòng có
  `IsRequired`.

Các nhóm scalar có cờ `Require...` tương ứng, ví dụ:

- `RequireRegion`;
- `RequirePriceRange`;
- `RequireAreaRange`;
- `RequireBedrooms`;
- `RequireMaxDistance`;
- `RequireFurnishing`;
- `RequirePets`;
- `RequireParking`;
- `RequireMoveInDate`;
- `RequireFloorRange`;
- `RequireDirection`;
- `RequireLeaseRange`;
- `RequireCategoryMatch`.

Không tạo cờ required cho tiêu chí chưa có giá trị. Handler lưu hồ sơ phải chuẩn
hóa cờ về `false` nếu tiêu chí tương ứng bị bỏ trống.

## Quy tắc lọc và xếp hạng

### Các kiểu sắp xếp

Giữ nguyên:

- mới nhất;
- giá tăng dần;
- giá giảm dần;
- diện tích lớn nhất;
- gần bạn.

Thêm:

- phù hợp nhất (`match_desc`).

Khi người dùng không chọn `Phù hợp nhất`, các bộ lọc nhanh giữ hành vi hiện có
để tránh regression. Khi chọn `Phù hợp nhất`, hệ thống áp dụng mô hình bắt
buộc/mong muốn.

Nếu `match_desc` không có bất kỳ tiêu chí hợp lệ nào, trang hiển thị thông báo
tiếng Việt ngắn và fallback về sắp xếp mới nhất.

### Loại theo điều kiện bắt buộc

Một căn bị loại nếu không đạt bất kỳ tiêu chí đã bật `Bắt buộc`:

- khu vực phải trùng;
- giá và diện tích phải nằm trong khoảng đã khai báo;
- số phòng ngủ phải đạt mức tối thiểu;
- loại hình phải thuộc một trong các loại được chấp nhận;
- mọi tiện ích được đánh dấu bắt buộc đều phải có;
- khoảng cách phải không vượt bán kính tối đa;
- mức nội thất phải bằng hoặc tốt hơn mức yêu cầu;
- nếu người dùng cần nuôi thú cưng thì căn phải cho phép;
- loại chỗ đậu xe phải bằng hoặc tốt hơn mức yêu cầu;
- ngày có thể vào ở của căn phải không muộn hơn ngày dự kiến vào ở;
- tầng thực tế phải nằm trong khoảng yêu cầu;
- hướng nhà phải trùng;
- khoảng thời hạn cho thuê của căn phải giao với khoảng thời hạn mong muốn.

Nếu một căn thiếu dữ liệu cần thiết cho tiêu chí bắt buộc, coi như không đạt.

### Chấm điểm tiêu chí mong muốn

Tách `RentalMatchScorer` thành lớp thuần, không phụ thuộc EF Core. Lớp này nhận
một candidate gọn và một preference draft rồi trả:

- `IsEligible`;
- `ScorePercent`;
- danh sách lý do phù hợp đã sắp thứ tự ưu tiên.

Mỗi tiêu chí mong muốn đã khai báo đóng góp một điểm bằng nhau. Điểm phần trăm:

`số tiêu chí mong muốn đạt / tổng số tiêu chí mong muốn hợp lệ * 100`.

Tiện ích mong muốn được tính riêng từng tiện ích. Nếu hồ sơ chỉ có điều kiện bắt
buộc và căn đã vượt qua toàn bộ điều kiện, điểm hiển thị là `100%`.

Sau khi chấm điểm:

1. sắp giảm dần theo phần trăm;
2. nếu bằng điểm, sắp mới nhất trước;
3. nếu vẫn bằng nhau, sắp ID cao hơn trước;
4. phân trang sau khi đã xếp hạng.

Lý do hiển thị trên card lấy tối đa ba mục có giá trị giải thích cao nhất, ưu
tiên vị trí, giá, loại hình, số phòng ngủ, nội thất, sau đó mới đến tiện ích.

### Cách thực thi query

Giữ lọc cơ bản và điều kiện bắt buộc có thể dịch sang SQL trong database. Với
`match_desc`, dùng query hai pha tương tự `Gần bạn`:

1. lọc trong database và project candidate gọn;
2. dùng `RentalMatchScorer` chấm điểm candidate trước khi phân trang;
3. lấy ID của trang cần hiển thị;
4. query card đầy đủ theo các ID đó;
5. khôi phục thứ tự đã chấm.

Cách này giữ scorer độc lập và test được. Nếu số lượng tin tăng lớn, có thể tối
ưu scorer sang SQL hoặc search index trong vòng sau; không thêm hạ tầng đó ở
vòng này.

## Hợp đồng request và trạng thái URL

Mở rộng query string trang thuê và API tìm kiếm với các tham số nullable tương
ứng preference draft. Danh sách chính cần hỗ trợ:

- filter hiện có;
- `minBedrooms`;
- `furnishingLevel`;
- `allowsPets`;
- `parkingType`;
- `availableBy`;
- `preferredAddress`, `preferredLatitude`, `preferredLongitude`;
- `maxDistanceKm`;
- `minFloor`, `maxFloor`;
- `houseDirection`;
- `minLeaseMonths`, `maxLeaseMonths`;
- `requiredCriteria`: danh sách lặp lại các key scalar được đánh dấu bắt buộc;
- `requiredAmenityIds`: danh sách ID tiện ích bắt buộc;
- `sort=match_desc`.

`requiredCriteria` chỉ nhận allowlist:

- `region`;
- `priceRange`;
- `areaRange`;
- `bedrooms`;
- `category`;
- `maxDistance`;
- `furnishing`;
- `pets`;
- `parking`;
- `moveInDate`;
- `floorRange`;
- `direction`;
- `leaseRange`.

Các key khác bị bỏ qua ở trang MVC và bị từ chối ở API JSON. Một key không có
giá trị tiêu chí tương ứng không có hiệu lực. Tiện ích mong muốn tiếp tục dùng
`amenityIds`; tiện ích bắt buộc phải xuất hiện trong cả `amenityIds` và
`requiredAmenityIds`.

Các link phân trang, thao tác đăng nhập quay lại và soft navigation phải giữ
query string hiện tại. Tọa độ vị trí ưu tiên chỉ được lưu vào database khi người
dùng chủ động bấm `Lưu hồ sơ nhu cầu`.

Luồng tiếp tục lưu sau đăng nhập:

1. JavaScript serialize filter form thành URL `/Home/Rentals` có query hiện tại
   và thêm `pendingPreferenceSave=1`.
2. Nếu chưa đăng nhập, nút lưu chuyển đến `/Home/Login` với URL trên làm
   `returnUrl`.
3. Sau khi đăng nhập thành công, trang thuê render lại đúng filter.
4. Initializer trang thuê thấy `pendingPreferenceSave=1`, tự gửi đúng một POST
   có anti-forgery đến endpoint lưu hồ sơ.
5. Endpoint lưu redirect về cùng URL sau khi bỏ `pendingPreferenceSave`.

POST lưu hồ sơ phải idempotent theo `UserId`: cập nhật profile hiện có hoặc tạo
profile lần đầu, không tạo dòng trùng. Nếu JavaScript bị tắt, trang hiển thị nút
`Hoàn tất lưu hồ sơ` để người dùng tự gửi POST.

API JSON giữ nguyên tính nghiêm ngặt: payload sai trả `400 Bad Request`. Trang
MVC chịu lỗi tốt hơn: bỏ qua giá trị sai, giữ trang sử dụng được và hiển thị
thông báo tiếng Việt.

## Component và trách nhiệm

### Server

- `Apartment`: lưu thuộc tính điều kiện thuê chuẩn hóa.
- `RentalPreferenceProfile` và bảng liên kết: lưu một hồ sơ đang hoạt động cho
  mỗi tài khoản.
- `RentalPreferenceDraft`: contract dùng chung cho query string, form lưu và
  scorer.
- `RentalMatchScorer`: loại điều kiện bắt buộc, tính phần trăm và lý do.
- `RentalPreferencesController`: lưu/cập nhật hồ sơ bằng POST và anti-forgery.
- `RentalsQueryHandler`: giữ filter cũ, thêm match mode hai pha.
- `HomeController.Rentals`: bind draft, nạp hồ sơ đã lưu và lookup cho sidebar.
- `CreateListingCommandHandler`: validate và lưu thuộc tính mới.
- `MyListingsController` và admin `ApartmentsController`: cho phép hiệu chỉnh
  thuộc tính mới.

### Browser

- Tách initializer drawer/filter của trang thuê thành JavaScript riêng, bind
  một lần trên `DOMContentLoaded` và `luxe:page-loaded`.
- Tái sử dụng pattern autocomplete Photon và OpenFreeMap từ form đăng tin cho
  địa chỉ ưu tiên. Tránh copy toàn bộ logic bằng helper dùng chung.
- Giữ `rentals-nearby.js` cho hành động `Gần bạn`.
- Khi quay lại sau đăng nhập với cờ lưu tạm, tự gửi đúng một POST có
  anti-forgery rồi điều hướng về URL đã bỏ cờ.

### View

- `Rentals.cshtml`: sidebar, drawer, nút mobile, profile actions, score và lý do.
- `PostListing.cshtml`: nhóm `Điều kiện thuê`.
- `MyListings/Edit.cshtml`: nhóm `Điều kiện thuê`.
- Admin apartment edit: nhóm `Điều kiện thuê`.
- Apartment detail: hiển thị tóm tắt điều kiện thuê để dữ liệu mới không chỉ
  tồn tại trong bộ lọc.

## Migration và đồng bộ dữ liệu

Tạo EF Core migration PostgreSQL:

- thêm cột điều kiện thuê vào `CanHo`;
- tạo bảng profile và hai bảng liên kết;
- tạo unique index trên `RentalPreferenceProfile.UserId`;
- thêm index cần thiết cho các cột lọc phổ biến.

Dữ liệu cũ được gán mặc định hợp lý để demo có thể dùng ngay:

- suy ra nội thất từ tiện ích `furniture`;
- suy ra đậu xe từ tiện ích `parking`;
- đặt `AllowsPets = false` nếu chưa biết;
- đặt ngày có thể vào ở bằng ngày migration hoặc ngày seed phù hợp;
- đặt khoảng thuê mặc định theo loại hình;
- tầng và hướng nhà để null nếu không có nguồn đáng tin cậy.

`SeedData`, `SampleListings` và `SampleHeroes` phải khai báo rõ các giá trị mới
để dữ liệu mẫu nhất quán và test có thể dự đoán kết quả.

## Validation và xử lý lỗi

- Từ chối enum ngoài phạm vi.
- Từ chối tầng âm hoặc `MinFloor > MaxFloor`.
- Từ chối thời hạn thuê nhỏ hơn `1` hoặc min lớn hơn max.
- Từ chối khoảng giá, diện tích và bán kính âm hoặc min lớn hơn max.
- Validate tọa độ bằng `GeoDistance.ValidatePair`.
- Không tính khoảng cách nếu chỉ có một tọa độ.
- Không fail toàn bộ trang vì một căn thiếu dữ liệu tùy chọn.
- Nếu Photon hoặc OpenFreeMap lỗi, giữ form sử dụng được và hiển thị thông báo
  tiếng Việt; người dùng vẫn có thể nhập địa chỉ thủ công.
- Giữ anti-forgery cho mọi POST.
- Chỉ local redirect đến URL nội bộ sau đăng nhập.

## Kiểm thử

### Unit test

- `RentalMatchScorer` loại căn không đạt từng loại điều kiện bắt buộc.
- `RentalMatchScorer` tính đúng phần trăm với nhiều tiêu chí mong muốn.
- Tiện ích mong muốn được tính riêng; tiện ích bắt buộc yêu cầu đủ tất cả.
- Chỉ có điều kiện bắt buộc thì căn hợp lệ nhận `100%`.
- Lý do được ưu tiên đúng và giới hạn tối đa ba mục.
- So sánh mức nội thất và chỗ đậu xe đúng thứ tự.
- Giao khoảng thời hạn thuê và khoảng tầng đúng.

### Integration test

- Migration tạo đúng profile một-một và bảng liên kết.
- Tạo tin lưu đủ thuộc tính mới.
- Tạo tin từ chối enum giả mạo, ngày sai, tầng âm và khoảng thuê sai.
- Chủ nhà sửa được thuộc tính mới của tin thuộc quyền sở hữu.
- Quản trị viên sửa được thuộc tính mới.
- Lưu hồ sơ lần đầu và cập nhật hồ sơ hiện có không tạo dòng thứ hai.
- `match_desc` loại điều kiện bắt buộc trước khi phân trang.
- `match_desc` sắp đúng điểm, thời gian tạo và ID.
- API từ chối query sai bằng `400`.
- Trang MVC bỏ qua query sai và hiển thị cảnh báo.
- Regression cho sort cũ, `Gần bạn`, phân trang, yêu thích và soft navigation.

### Playwright trình duyệt thật

- Desktop: sidebar trái và danh sách ba cột.
- Tablet: danh sách hai cột.
- Mobile: mở và đóng drawer toàn màn hình.
- Khách chưa đăng nhập lọc thử với `Phù hợp nhất`.
- Bấm lưu khi chưa đăng nhập, đăng nhập và quay lại đúng filter trước khi lưu.
- Tài khoản chủ nhà cũng lưu và áp dụng hồ sơ được.
- Drawer nâng cao giữ trạng thái công tắc `Bắt buộc`.
- Autocomplete Photon và OpenFreeMap cập nhật vị trí ưu tiên.
- Form đăng tin validate và lưu nhóm `Điều kiện thuê`.
- Card phù hợp hiển thị phần trăm và tối đa ba lý do.
- Smoke test lại ít nhất mười màn hình hiện có.

## Tiêu chí nghiệm thu

- Trang thuê desktop có sidebar trái và giữ ba card trên mỗi hàng.
- Mọi nội dung mới phía người dùng đều là tiếng Việt có dấu.
- Khách chưa đăng nhập lọc thử và dùng `Phù hợp nhất` được.
- Sau đăng nhập, thao tác lưu dở được tiếp tục mà không mất filter.
- Mỗi tài khoản chỉ có một hồ sơ nhu cầu đang hoạt động.
- Tiêu chí bắt buộc loại căn không đạt; tiêu chí mong muốn quyết định điểm.
- Card match mode hiển thị phần trăm và không quá ba lý do.
- Tin đăng mới, màn sửa tin chủ nhà và màn sửa admin dùng cùng thuộc tính.
- Dữ liệu mẫu cũ có giá trị hợp lý để demo kết quả ngay.
- Các sort cũ, `Gần bạn`, yêu thích, phân trang và soft navigation không bị
  regression.
