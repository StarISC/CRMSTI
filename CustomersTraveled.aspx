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
        #traveledTable_length {
            display: none;
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
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">Passport</label>
                        <input id="txtPassport" class="form-control" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label fw-semibold">H&#7885; v&#224; t&#234;n</label>
                        <input id="txtName" class="form-control" />
                    </div>
                    <div class="col-md-3">
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
                    <div class="col-md-1 d-flex gap-2 justify-content-end flex-nowrap">
                        <button id="btnFilter" class="btn btn-primary" type="button">L&#7885;c</button>
                        <button id="btnReset" class="btn btn-outline-secondary" type="button">X&#243;a l&#7885;c</button>
                    </div>
                </div>
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
                    { data: 'Passport', className: 'nowrap' },
                    { data: 'CustomerName' },
                    { data: 'Gender' },
                    { data: 'Birthday', className: 'nowrap' },
                    { data: 'Phone', className: 'nowrap' },
                    { data: 'Countries', render: function (data) {
                        if (!data) return '';
                        return data.split(',').map(function (x) { return x.trim(); }).filter(Boolean)
                            .map(function (x) { return '<span class=\"tag-country\">' + $('<div/>').text(x).html() + '</span>'; }).join('');
                    } },
                    { data: 'TripCount', className: 'nowrap' },
                    { data: null, className: 'nowrap', render: function (data, type, row) {
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
</asp:Content>
