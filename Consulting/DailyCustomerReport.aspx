<%@ Page Language="C#" AutoEventWireup="true" CodeFile="DailyCustomerReport.aspx.cs" Inherits="Consulting_DailyCustomerReport" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Báo cáo khách hàng hằng ngày</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .report-title { font-weight: 700; color: #1d2353; }
        .filter-label { font-weight: 600; }
        .text-nowrap { white-space: nowrap; }
        .grid-view th { white-space: nowrap; }
        .grid-view td { vertical-align: top; }
        .cell-muted { color: #6b7280; }
        .cell-wrap { white-space: normal; }
        .cell-nowrap { white-space: nowrap; }
        .badge-yes {
            background: #dcfce7;
            border: 1px solid #bbf7d0;
            color: #166534;
            padding: 2px 8px;
            border-radius: 999px;
            font-weight: 700;
            font-size: 12px;
        }
        .badge-no {
            background: #fee2e2;
            border: 1px solid #fecaca;
            color: #991b1b;
            padding: 2px 8px;
            border-radius: 999px;
            font-weight: 700;
            font-size: 12px;
        }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm mb-3">
        <div class="card-body">
            <h4 class="report-title mb-3">Báo cáo khách hàng hằng ngày</h4>
            <div class="row g-2 align-items-end">
                <div class="col-sm-3 col-md-2">
                    <label class="form-label filter-label">Từ ngày</label>
                    <asp:TextBox ID="txtFromDate" runat="server" TextMode="Date" CssClass="form-control" />
                </div>
                <div class="col-sm-3 col-md-2">
                    <label class="form-label filter-label">Đến ngày</label>
                    <asp:TextBox ID="txtToDate" runat="server" TextMode="Date" CssClass="form-control" />
                </div>
                <div class="col-sm-6 col-md-4">
                    <label class="form-label filter-label">Từ khóa</label>
                    <asp:TextBox ID="txtKeyword" runat="server" CssClass="form-control" placeholder="Tên, điện thoại, ghi chú..." />
                </div>
                <div class="col-sm-6 col-md-4 d-flex gap-2">
                    <asp:Button ID="btnApplyFilter" runat="server" Text="Lọc" CssClass="btn btn-primary" />
                    <asp:Button ID="btnClearFilter" runat="server" Text="Xóa lọc" CssClass="btn btn-outline-secondary" />
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm">
        <div class="card-body">
            <div class="table-responsive">
                <table id="dailyReportTable" class="table table-sm table-striped align-middle mb-0 grid-view" style="width:100%">
                    <thead></thead>
                </table>
            </div>
        </div>
    </div>
    <script>
        function escapeHtml(value) {
            return $('<div/>').text(value || '').html();
        }

        function formatJsonDate(value) {
            if (!value) return '';
            var match = /Date\((\d+)\)/.exec(value);
            if (match) {
                var d = new Date(parseInt(match[1], 10));
                return d.toLocaleDateString('vi-VN') + ' ' + d.toLocaleTimeString('vi-VN');
            }
            return value;
        }

        function buildColumns(columns) {
            return columns.map(function (c) {
                var col = { data: c };
                var key = (c || '').toLowerCase();
                if (key === 'createdat') {
                    col.className = 'cell-nowrap';
                    col.render = function (data) { return escapeHtml(formatJsonDate(data)); };
                } else if (key === 'facebooklink') {
                    col.render = function (data) {
                        if (!data) return '<span class="cell-muted">-</span>';
                        var safe = escapeHtml(data);
                        return '<a href="' + safe + '" target="_blank" rel="noopener noreferrer">' + safe + '</a>';
                    };
                } else if (key === 'isstat') {
                    col.className = 'cell-nowrap';
                    col.render = function (data) {
                        var val = (data === true || data === 'true' || data === 1 || data === '1');
                        return val ? '<span class="badge-yes">Có</span>' : '<span class="badge-no">Không</span>';
                    };
                } else if (key === 'consultingcontent') {
                    col.className = 'cell-wrap';
                    col.render = function (data) { return escapeHtml(data); };
                } else if (key === 'email') {
                    col.className = 'cell-nowrap';
                }
                return col;
            });
        }

        function buildColumnDefs(columns) {
            var defs = [];
            columns.forEach(function (c, idx) {
                if (!c) return;
                var key = c.toLowerCase();
                if (key === 'provinceid') {
                    defs.push({ targets: idx, visible: false, searchable: false });
                }
            });
            return defs;
        }

        function renameHeaders(columns) {
            var map = {
                id: 'ID',
                provincename: 'Tỉnh',
                provinceid: 'ID tỉnh',
                fullname: 'Họ tên',
                address: 'Địa chỉ',
                email: 'Email',
                consultingcontent: 'Nội dung tư vấn',
                facebooklink: 'Facebook',
                createdat: 'Ngày tạo',
                createdby: 'Người tạo',
                isstat: 'Đã chăm sóc'
            };
            return columns.map(function (c) {
                var key = (c || '').toLowerCase();
                return map[key] || c;
            });
        }

        function loadDailyReportTable() {
            var fromDate = $('#<%=txtFromDate.ClientID%>').val();
            var toDate = $('#<%=txtToDate.ClientID%>').val();
            var keyword = $('#<%=txtKeyword.ClientID%>').val();

            $.getJSON('DailyCustomerReportApi.aspx', { init: 1, fromDate: fromDate, toDate: toDate, keyword: keyword })
                .done(function (resp) {
                    var cols = resp && resp.columns ? resp.columns : [];
                    var headerNames = renameHeaders(cols);
                    var $thead = $('#dailyReportTable thead');
                    $thead.empty();
                    var $tr = $('<tr/>');
                    headerNames.forEach(function (c) { $tr.append('<th>' + escapeHtml(c) + '</th>'); });
                    $thead.append($tr);

                    if ($.fn.DataTable.isDataTable('#dailyReportTable')) {
                        $('#dailyReportTable').DataTable().destroy();
                    }

                    $('#dailyReportTable').DataTable({
                        processing: true,
                        serverSide: true,
                        searching: false,
                        ajax: {
                            url: 'DailyCustomerReportApi.aspx',
                            type: 'POST',
                            data: function (d) {
                                d.fromDate = $('#<%=txtFromDate.ClientID%>').val();
                                d.toDate = $('#<%=txtToDate.ClientID%>').val();
                                d.keyword = $('#<%=txtKeyword.ClientID%>').val();
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
                        lengthMenu: [[20, 50, 100, 200], [20, 50, 100, 200]],
                        columns: buildColumns(cols),
                        columnDefs: buildColumnDefs(cols)
                    });
                })
                .fail(function () {
                    alert('Không lấy được cấu trúc dữ liệu.');
                });
        }

        $(function () {
            $('#<%=btnApplyFilter.ClientID%>, #<%=btnClearFilter.ClientID%>').attr('type', 'button');
            $('#<%=btnApplyFilter.ClientID%>').on('click', function () {
                loadDailyReportTable();
            });
            $('#<%=btnClearFilter.ClientID%>').on('click', function () {
                $('#<%=txtFromDate.ClientID%>').val('');
                $('#<%=txtToDate.ClientID%>').val('');
                $('#<%=txtKeyword.ClientID%>').val('');
                loadDailyReportTable();
            });

            loadDailyReportTable();
        });
    </script>
</asp:Content>
