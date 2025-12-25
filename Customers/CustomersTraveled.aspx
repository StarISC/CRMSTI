<%@ Page Language="C#" AutoEventWireup="true" CodeFile="CustomersTraveled.aspx.cs" Inherits="CustomersTraveled" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Kh&#225;ch &#273;&#227; &#273;i tour</asp:Content>
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
                    <h2 class="h4 mb-1 text-primary">Kh&#225;ch &#273;&#227; &#273;i tour</h2>
                    <div class="text-muted">Danh s&#225;ch kh&#225;ch h&#224;ng tr&#249;ng passport &#273;&#227; &#273;&#432;&#7907;c g&#7897;p l&#7841;i</div>
                </div>
            </div>
            <div class="mb-3">
                <div class="row g-3 align-items-end flex-nowrap">
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Passport</label>
                        <input id="txtPassport" class="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">H&#7885; v&#224; t&#234;n</label>
                        <input id="txtName" class="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">&#272;i&#7879;n tho&#7841;i</label>
                        <input id="txtPhone" class="form-control" />
                    </div>
                    <div class="col-md-2">
                        <label class="form-label fw-semibold">Th&#225;ng sinh</label>
                        <select id="ddlBirthMonth" class="form-select">
                            <option value="">T&#7845;t c&#7843;</option>
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
                    <div class="col-md-3 d-flex gap-2 justify-content-end flex-nowrap">
                        <button id="btnFilter" class="btn btn-primary" type="button">L&#7885;c</button>
                        <button id="btnReset" class="btn btn-outline-secondary" type="button">X&#243;a l&#7885;c</button>
                    </div>
                </div>
            </div>
            <div class="mb-2 small text-muted">
                Ghi ch&#250;:
                <span class="tag-phone">S&#7889; kh&#225;ch h&#224;ng &#273;&#432;&#7907;c cung c&#7845;p</span>
                <span class="tag-phone tag-phone-external">S&#7889; ng&#432;&#7901;i &#273;&#7863;t tour</span>
            </div>
            <div class="table-responsive">
                <table id="traveledTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>Passport</th>
                            <th>Kh&#225;ch h&#224;ng</th>
                            <th>Gi&#7899;i t&#237;nh</th>
                            <th>Ng&#224;y sinh</th>
                            <th>&#272;i&#7879;n tho&#7841;i</th>
                            <th>&#272;&#227; &#273;i &#273;&#7871;n</th>
                            <th>S&#7889; l&#7847;n &#273;i</th>
                            <th>L&#7847;n &#273;i g&#7847;n nh&#7845;t</th>
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
                    },
                    dataSrc: function (json) {
                        if (json.error) {
                            alert('L&#7895;i t&#7843;i d&#7919; li&#7879;u: ' + json.error);
                            return [];
                        }
                        return json.data;
                    }
                },
                pageLength: 50,
                lengthMenu: [[20, 30, 50, 100, 200], [20, 30, 50, 100, 200]],
                columns: [
                    { data: 'Passport', className: 'nowrap', defaultContent: '', render: function (data) {
                        if (!data) return '';
                        var safe = $('<div/>').text(data).html();
                        return '<a href="#" class="passport-link" data-passport="' + safe + '">' + safe + '</a>';
                    } },
                    { data: 'CustomerName', defaultContent: '' },
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
                            .map(function (x) { return '<span class=\"tag-country\">' + $('<div/>').text(x).html() + '</span>'; }).join('');
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
            $('#traveledTable').on('click', '.passport-link', function (e) {
                e.preventDefault();
                var passport = $(this).data('passport');
                loadPassportTours(passport);
            });
            $('#btnFilter').on('click', function () { table.ajax.reload(); });
            $('#btnReset').on('click', function () {
                $('#txtPassport').val('');
                $('#txtName').val('');
                $('#txtPhone').val('');
                $('#ddlBirthMonth').val('');
                table.ajax.reload();
            });
        });
    </script>

    <div class="modal fade" id="passportToursModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">L&#7883;ch s&#7917; tour &#273;&#227; &#273;i</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div id="passportToursLoading" class="d-flex justify-content-center my-3" style="display:none;">
                        <div class="spinner-border text-primary" role="status"></div>
                    </div>
                    <div id="passportToursContent" style="display:none;">
                        <div class="mb-2 text-muted">Passport: <span id="passportLabel"></span></div>
                        <div id="passportToursInfo" class="mb-2"></div>
                        <div class="table-responsive">
                            <table class="table table-sm table-striped align-middle mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th>TT</th>
                                        <th>Code</th>
                                        <th>Ship</th>
                                        <th>Ng&#224;y KH</th>
                                        <th>Qu&#7889;c gia</th>
                                        <th>&#272;&#7863;t b&#7903;i</th>
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
        function loadPassportTours(passport) {
            var modalEl = document.getElementById('passportToursModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            $('#passportToursError').hide();
            $('#passportToursContent').hide();
            $('#passportToursBody').empty();
            $('#passportToursLoading').show();
            $('#passportLabel').text(passport || '');
            modal.show();
            $.ajax({
                url: 'CustomersTraveledDetailApi.aspx',
                type: 'GET',
                dataType: 'json',
                data: { passport: passport },
                success: function (resp) {
                    if (resp && resp.error) {
                        $('#passportToursError').text(resp.error).show();
                        return;
                    }
                    if (resp) {
                        $('#passportLabel').text(resp.Passport || passport || '');
                        var infoHtml = '';
                        if (resp.CustomerName || resp.Phone || resp.Gender || resp.Birthday || resp.TripCount || resp.LatestDeparture || resp.LatestCode || resp.Countries) {
                            var countries = resp.Countries ? resp.Countries.split(',').map(function (x) { return x.trim(); }).filter(Boolean)
                                .map(function (x) { return '<span class="tag-country">' + $('<div/>').text(x).html() + '</span>'; }).join('') : '';
                            var latestTour = (resp.LatestDeparture || '') + (resp.LatestCode ? '<br><div class=\"tag-code\">' + $('<div/>').text(resp.LatestCode).html() + '</div>' : '');
                            infoHtml = ''
                                + '<div class=\"row g-2 mb-2\">'
                                + '<div class=\"col-md-4\"><div class=\"fw-semibold\">H&#7885; t&#234;n</div><div>' + (resp.CustomerName || '') + '</div></div>'
                                + '<div class=\"col-md-4\"><div class=\"fw-semibold\">Gi&#7899;i t&#237;nh</div><div>' + (resp.Gender || '') + '</div></div>'
                                + '<div class=\"col-md-4\"><div class=\"fw-semibold\">Ng&#224;y sinh</div><div>' + (resp.Birthday || '') + '</div></div>'
                                + '</div>'
                                + '<div class=\"row g-2 mb-2\">'
                                + '<div class=\"col-md-4\"><div class=\"fw-semibold\">&#272;i&#7879;n tho&#7841;i</div><div>' + (resp.Phone || '') + '</div></div>'
                                + '<div class=\"col-md-4\"><div class=\"fw-semibold\">S&#7889; l&#7847;n &#273;i</div><div>' + (resp.TripCount || '0') + '</div></div>'
                                + '<div class=\"col-md-4\"><div class=\"fw-semibold\">L&#7847;n &#273;i g&#7847;n nh&#7845;t</div><div>' + latestTour + '</div></div>'
                                + '</div>'
                                + '<div class=\"mb-2\"><div class=\"fw-semibold\">Qu&#7889;c gia &#273;&#227; &#273;i</div><div>' + countries + '</div></div>';
                        }
                        $('#passportToursInfo').html(infoHtml);
                    }
                    var rows = '';
                    if (resp && resp.Orders && resp.Orders.length) {
                        $.each(resp.Orders, function (i, row) {
                            var countries = row.Countries ? row.Countries.split(',').map(function (x) { return x.trim(); }).filter(Boolean)
                                .map(function (x) { return '<span class="tag-country">' + $('<div/>').text(x).html() + '</span>'; }).join('') : '';
                            var statusBadge = row.Status ? '<span class="tag-status tag-status-' + row.Status + '">' + $('<div/>').text(row.Status).html() + '</span>' : '';
                            var booker = (row.Phone || '') + '<br><small class=\"text-muted\">' + (row.CreatedBy || '') + '</small>';
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
                        rows = '<tr><td colspan="6" class="text-center text-muted">Ch&#432;a c&#243; d&#7919; li&#7879;u</td></tr>';
                    }
                    $('#passportToursBody').html(rows);
                    $('#passportToursContent').show();
                },
                error: function () {
                    $('#passportToursError').text('L&#7895;i t&#7843;i chi ti&#7871;t tour').show();
                },
                complete: function () {
                    $('#passportToursLoading').hide();
                }
            });
        }
    </script>
</asp:Content>
