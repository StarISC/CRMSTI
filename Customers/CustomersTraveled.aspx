<%@ Page Language="C#" AutoEventWireup="true" CodeFile="CustomersTraveled.aspx.cs" Inherits="CustomersTraveled" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Khách đã đi tour</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        th {
            white-space: nowrap;
        }
        .nowrap {
            white-space: nowrap;
        }
        .loading-overlay {
            position: fixed;
            inset: 0;
            background: rgba(255, 255, 255, 0.6);
            display: none;
            align-items: center;
            justify-content: center;
            z-index: 1050;
        }
        .tag-code {
            display: inline-block;
            background: #fff3cd;
            color: #8a4b0f;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 700;
            margin-top: 4px;
            border: 1px solid #ffe4a3;
            font-size: 12px;
        }
        .tag-country {
            display: inline-block;
            background: #eef2ff;
            color: #1d2353;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 600;
            margin: 2px 4px 2px 0;
            border: 1px solid #dbe4ff;
            font-size: 12px;
        }
        .tag-passport {
            display: inline-block;
            background: #f3f4f6;
            color: #111827;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 600;
            margin: 2px 4px 2px 0;
            border: 1px solid #e5e7eb;
            font-size: 12px;
        }
        .tag-phone {
            display: inline-block;
            background: #ecfdf3;
            color: #0f5132;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 600;
            margin: 2px 4px 2px 0;
            border: 1px solid #c9f2dc;
            font-size: 12px;
        }
        .tag-phone-external {
            background: #fff1f2;
            color: #9f1239;
            border-color: #fecdd3;
        }
        .tag-status {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 700;
            font-size: 12px;
            border: 1px solid transparent;
        }
        .tag-status-OP {
            background: #e2e8f0;
            color: #1f2937;
            border-color: #cbd5e1;
        }
        .tag-status-CX {
            background: #fee2e2;
            color: #991b1b;
            border-color: #fecaca;
        }
        .tag-status-BK {
            background: #fef3c7;
            color: #92400e;
            border-color: #fde68a;
        }
        .tag-status-FP {
            background: #dcfce7;
            color: #166534;
            border-color: #bbf7d0;
        }
        #traveledTable_length {
            display: none;
        }
        #passportToursLoading {
            display: none !important;
        }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm">
        <div class="card-body">
            <div class="d-flex flex-column flex-lg-row justify-content-between align-items-lg-center mb-3">
                <div>
                    <h2 class="h4 mb-1 text-primary">Khách đã đi tour</h2>
                    <div class="text-muted">Danh sách khách hàng được gộp theo họ tên và ngày sinh</div>
                </div>
            </div>
            <div class="mb-3">
                <div class="row g-3 align-items-end">
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Passport</label>
                        <input id="txtPassport" class="form-control" />
                    </div>
                    <div class="col-md-4">
                        <label class="form-label fw-semibold">Họ và tên</label>
                        <input id="txtName" class="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Điện thoại</label>
                        <input id="txtPhone" class="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Tháng sinh</label>
                        <select id="ddlBirthMonth" class="form-select">
                            <option value="">Tất cả</option>
                            <option value="1">1</option>
                            <option value="2">2</option>
                            <option value="3">3</option>
                            <option value="4">4</option>
                            <option value="5">5</option>
                            <option value="6">6</option>
                            <option value="7">7</option>
                            <option value="8">8</option>
                            <option value="9">9</option>
                            <option value="10">10</option>
                            <option value="11">11</option>
                            <option value="12">12</option>
                        </select>
                    </div>
                </div>
                <div class="row g-3 align-items-end mt-0">
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Từ ngày</label>
                        <input id="txtFromDate" type="date" class="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Đến ngày</label>
                        <input id="txtToDate" type="date" class="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Số lần đi từ</label>
                        <input id="txtTripFrom" type="number" min="1" class="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Đến</label>
                        <input id="txtTripTo" type="number" min="1" class="form-control" />
                    </div>
                    <div class="col-12 d-flex gap-2 justify-content-end flex-nowrap">
                        <button id="btnFilter" class="btn btn-primary" type="button">Lọc</button>
                        <button id="btnReset" class="btn btn-outline-secondary" type="button">Xóa lọc</button>
                        <a id="btnExport" runat="server" class="btn btn-success btn-sm" title="Export Excel" href="#">
                            <i class="bi bi-file-earmark-excel"></i>
                        </a>
                    </div>
                </div>
            </div>
            <div class="mb-2 small text-muted">
                Ghi chú:
                <span class="tag-phone">Số khách hàng được cung cấp</span>
                <span class="tag-phone tag-phone-external">Số người đặt tour</span>
            </div>
            <div class="table-responsive">
                <table id="traveledTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>Passport</th>
                            <th>Khách hàng</th>
                            <th>Giới tính</th>
                            <th>Ngày sinh</th>
                            <th>Điện thoại</th>
                            <th>Đã đi đến</th>
                            <th>Số lần đi</th>
                            <th>Lần đi gần nhất</th>
                        </tr>
                    </thead>
                    <tbody></tbody>
                </table>
            </div>
        </div>
    </div>
    <div id="traveledLoading" class="loading-overlay">
        <div class="spinner-border text-primary" role="status" aria-hidden="true"></div>
    </div>

    <script>
        $(function () {
            var table = $('#traveledTable').DataTable({
                processing: true,
                serverSide: true,
                searching: false,
                ajax: {
                    url: 'CustomersTraveledApi.aspx',
                    type: 'POST',
                    data: function (d) {
                        d.passport = $('#txtPassport').val();
                        d.customerName = $('#txtName').val();
                        d.phone = $('#txtPhone').val();
                        d.birthMonth = $('#ddlBirthMonth').val();
                        d.fromDate = $('#txtFromDate').val();
                        d.toDate = $('#txtToDate').val();
                        d.tripFrom = $('#txtTripFrom').val();
                        d.tripTo = $('#txtTripTo').val();
                    },
                    dataSrc: function (json) {
                        if (json.error) {
                            alert('Lỗi tải dữ liệu: ' + json.error);
                            return [];
                        }
                        return json.data;
                    }
                },
                pageLength: 50,
                lengthMenu: [[20, 30, 50, 100, 200], [20, 30, 50, 100, 200]],
                columns: [
                    { data: 'Passports', defaultContent: '', render: function (data) {
                        if (!data) return '';
                        var items = data.split(';').map(function (x) { return x.trim(); }).filter(Boolean);
                        return items.map(function (x, i) {
                            var prefix = (i > 0 && i % 2 === 0) ? '<br>' : '';
                            return prefix + '<span class="tag-passport">' + $('<div/>').text(x).html() + '</span>';
                        }).join('');
                    } },
                    { data: null, className: 'nowrap', defaultContent: '', render: function (data, type, row) {
                        var name = row.CustomerName ? $('<div/>').text(row.CustomerName).html() : '';
                        var birthday = row.Birthday ? $('<div/>').text(row.Birthday).html() : '';
                        return '<a href="#" class="customer-link" data-name="' + name + '" data-birthday="' + birthday + '">' + name + '</a>';
                    } },
                    { data: 'Gender', defaultContent: '' },
                    { data: 'Birthday', className: 'nowrap', defaultContent: '' },
                    { data: 'Phone', defaultContent: '', render: function (data) {
                        if (!data) return '';
                        return data.split(';').map(function (x) { return x.trim(); }).filter(Boolean)
                            .map(function (item) {
                                var parts = item.split('||');
                                var phone = parts[0] || '';
                                var source = parts[1] || 'customer';
                                var cls = source === 'order' ? 'tag-phone tag-phone-external' : 'tag-phone';
                                return '<span class="' + cls + '">' + $('<div/>').text(phone).html() + '</span>';
                            }).join('');
                    } },
                    { data: 'Countries', defaultContent: '', render: function (data) {
                        if (!data) return '';
                        return data.split(',').map(function (x) { return x.trim(); }).filter(Boolean)
                            .map(function (x) { return '<span class="tag-country">' + $('<div/>').text(x).html() + '</span>'; }).join('');
                    } },
                    { data: 'TripCount', className: 'nowrap', defaultContent: '0' },
                    { data: null, className: 'nowrap', defaultContent: '', render: function (data, type, row) {
                        var dateLine = row.LatestCreation || '';
                        var codeLine = row.LatestCode ? '<br><div class="tag-code">' + $('<div/>').text(row.LatestCode).html() + '</div>' : '';
                        return dateLine + codeLine;
                    } }
                ]
            });
            table.on('page.dt draw.dt', function () {
                $('html, body').animate({ scrollTop: 0 }, 200);
            });
            table.on('processing.dt', function (e, settings, processing) {
                $('#traveledLoading').css('display', processing ? 'flex' : 'none');
            });
            $('#traveledTable').on('click', '.customer-link', function (e) {
                e.preventDefault();
                var name = $(this).data('name') || '';
                var birthday = $(this).data('birthday') || '';
                loadPassportTours(name, birthday);
            });
            $('#btnFilter').on('click', function () { table.ajax.reload(); });
            $('#btnReset').on('click', function () {
                $('#txtPassport').val('');
                $('#txtName').val('');
                $('#txtPhone').val('');
                $('#ddlBirthMonth').val('');
                $('#txtFromDate').val('');
                $('#txtToDate').val('');
                $('#txtTripFrom').val('');
                $('#txtTripTo').val('');
                table.ajax.reload();
            });
            $('#<%=btnExport.ClientID%>').on('click', function (e) {
                e.preventDefault();
                var url = 'CustomersTraveledExport.aspx?'
                    + 'passport=' + encodeURIComponent($('#txtPassport').val() || '')
                    + '&customerName=' + encodeURIComponent($('#txtName').val() || '')
                    + '&phone=' + encodeURIComponent($('#txtPhone').val() || '')
                    + '&birthMonth=' + encodeURIComponent($('#ddlBirthMonth').val() || '')
                    + '&fromDate=' + encodeURIComponent($('#txtFromDate').val() || '')
                    + '&toDate=' + encodeURIComponent($('#txtToDate').val() || '')
                    + '&tripFrom=' + encodeURIComponent($('#txtTripFrom').val() || '')
                    + '&tripTo=' + encodeURIComponent($('#txtTripTo').val() || '');
                window.location.href = url;
            });
        });
    </script>

    <div class="modal fade" id="passportToursModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Lịch sử tour đã đi</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div id="passportToursLoading" class="d-flex justify-content-center my-3" style="display:none;">
                        <div class="spinner-border text-primary" role="status"></div>
                    </div>
                    <div id="passportToursContent" style="display:none;">
                        <div class="mb-2 text-muted">Khách hàng: <span id="passportLabel"></span></div>
                        <div id="passportToursInfo" class="mb-2"></div>
                        <div class="table-responsive">
                            <table class="table table-sm table-striped align-middle mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th>TT</th>
                                        <th>Code</th>
                                        <th>Ship</th>
                                        <th>Ngày KH</th>
                                        <th>Quốc gia</th>
                                        <th>Đặt bởi</th>
                                    </tr>
                                </thead>
                                <tbody id="passportToursBody"></tbody>
                            </table>
                        </div>
                    </div>
                    <div id="passportToursError" class="text-danger" style="display:none;"></div>
                </div>
            </div>
        </div>
    </div>

    <script>
        function loadPassportTours(name, birthday) {
            var modalEl = document.getElementById('passportToursModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            $('#passportToursError').hide();
            $('#passportToursContent').hide();
            $('#passportToursBody').empty();
            $('#passportToursLoading').show();
            $('#passportLabel').text(name || '');
            modal.show();
            $.ajax({
                url: 'CustomersTraveledDetailApi.aspx',
                type: 'GET',
                dataType: 'json',
                data: { customerName: name, birthday: birthday },
                success: function (resp) {
                    if (resp && resp.error) {
                        $('#passportToursError').text(resp.error).show();
                        return;
                    }
                    if (resp) {
                        $('#passportLabel').text(resp.CustomerName || name || '');
                        var infoHtml = '';
                        if (resp.CustomerName || resp.Phone || resp.Gender || resp.Birthday || resp.TripCount || resp.LatestDeparture || resp.LatestCode || resp.Countries) {
                            var countries = resp.Countries ? resp.Countries.split(',').map(function (x) { return x.trim(); }).filter(Boolean)
                                .map(function (x) { return '<span class="tag-country">' + $('<div/>').text(x).html() + '</span>'; }).join('') : '';
                            var latestTour = (resp.LatestDeparture || '') + (resp.LatestCode ? '<br><div class="tag-code">' + $('<div/>').text(resp.LatestCode).html() + '</div>' : '');
                            infoHtml = ''
                                + '<div class="row g-2 mb-2">'
                                + '<div class="col-md-4"><div class="fw-semibold">Họ tên</div><div>' + (resp.CustomerName || '') + '</div></div>'
                                + '<div class="col-md-4"><div class="fw-semibold">Giới tính</div><div>' + (resp.Gender || '') + '</div></div>'
                                + '<div class="col-md-4"><div class="fw-semibold">Ngày sinh</div><div>' + (resp.Birthday || '') + '</div></div>'
                                + '</div>'
                                + '<div class="row g-2 mb-2">'
                                + '<div class="col-md-4"><div class="fw-semibold">Điện thoại</div><div>' + (resp.Phone || '') + '</div></div>'
                                + '<div class="col-md-4"><div class="fw-semibold">Số lần đi</div><div>' + (resp.TripCount || '0') + '</div></div>'
                                + '<div class="col-md-4"><div class="fw-semibold">Lần đi gần nhất</div><div>' + latestTour + '</div></div>'
                                + '</div>'
                                + '<div class="mb-2"><div class="fw-semibold">Quốc gia đã đi</div><div>' + countries + '</div></div>';
                        }
                        $('#passportToursInfo').html(infoHtml);
                    }
                    var rows = '';
                    if (resp && resp.Orders && resp.Orders.length) {
                        $.each(resp.Orders, function (i, row) {
                            var countries = row.Countries ? row.Countries.split(',').map(function (x) { return x.trim(); }).filter(Boolean)
                                .map(function (x) { return '<span class="tag-country">' + $('<div/>').text(x).html() + '</span>'; }).join('') : '';
                            var statusBadge = row.Status ? '<span class="tag-status tag-status-' + row.Status + '">' + $('<div/>').text(row.Status).html() + '</span>' : '';
                            var booker = (row.Phone || '') + '<br><small class="text-muted">' + (row.CreatedBy || '') + '</small>';
                            rows += '<tr>'
                                + '<td>' + statusBadge + '</td>'
                                + '<td>' + (row.Code || '') + '</td>'
                                + '<td>' + (row.ShipName || '') + '</td>'
                                + '<td>' + (row.DepartureDate || '') + '</td>'
                                + '<td>' + countries + '</td>'
                                + '<td>' + booker + '</td>'
                                + '</tr>';
                        });
                    } else {
                        rows = '<tr><td colspan="6" class="text-center text-muted">Chưa có dữ liệu</td></tr>';
                    }
                    $('#passportToursBody').html(rows);
                    $('#passportToursContent').show();
                },
                error: function () {
                    $('#passportToursError').text('Lỗi tải chi tiết tour').show();
                },
                complete: function () {
                    $('#passportToursLoading').hide();
                }
            });
        }
    </script>
</asp:Content>
