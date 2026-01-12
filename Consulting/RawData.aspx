<%@ Page Language="C#" AutoEventWireup="true" CodeFile="RawData.aspx.cs" Inherits="Consulting_RawData" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Dữ liệu thô</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .form-section-title { font-weight: 700; color: #1d2353; }
        .form-hint { color: #64748b; }
        #rawDataTable thead th { white-space: nowrap; }
        #rawDataTable tbody td:nth-child(2),
        #rawDataTable tbody td:nth-child(3),
        #rawDataTable tbody td:nth-child(4),
        #rawDataTable tbody td:nth-child(5) {
            white-space: nowrap;
        }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm">
        <div class="card-body">
            <h4 class="form-section-title mb-2">Dữ liệu thô</h4>
            <p class="form-hint mb-4">Nhập thông tin từ từ chuẩn bị lấy dữ liệu liên hệ.</p>

            <div class="row g-3">
                <div class="col-md-4">
                    <label class="form-label fw-semibold">Tên tỉnh</label>
                    <asp:TextBox ID="txtProvince" runat="server" CssClass="form-control" placeholder="VD: TP. Hồ Chí Minh" />
                </div>
                <div class="col-md-8">
                    <label class="form-label fw-semibold">Đường link</label>
                    <asp:TextBox ID="txtSourceUrl" runat="server" CssClass="form-control" placeholder="https://..." />
                </div>
                <div class="col-md-2">
                    <label class="form-label fw-semibold">Trang từ</label>
                    <asp:TextBox ID="txtPageFrom" runat="server" CssClass="form-control" placeholder="1" />
                </div>
                <div class="col-md-2">
                    <label class="form-label fw-semibold">Trang đến</label>
                    <asp:TextBox ID="txtPageTo" runat="server" CssClass="form-control" placeholder="10" />
                </div>
                <div class="col-md-8 d-flex align-items-end gap-2">
                    <asp:Button ID="btnSubmit" runat="server" Text="Tải HTML" CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
                    <asp:Button ID="btnClear" runat="server" Text="Xóa" CssClass="btn btn-outline-secondary" OnClick="btnClear_Click" />
                </div>
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Bộ lọc</label>
                    <asp:TextBox ID="txtResultFilter" runat="server" CssClass="form-control" placeholder="Tên công ty, MST, đại diện, điện thoại..." />
                </div>
                <div class="col-md-6 d-flex align-items-end gap-2">
                    <button type="button" id="btnApplyFilter" class="btn btn-dark">Lọc kết quả</button>
                    <button type="button" id="btnClearFilter" class="btn btn-outline-secondary">Xóa lọc</button>
                </div>
                <div class="col-12">
                    <asp:Label ID="lblStatus" runat="server" CssClass="text-muted small"></asp:Label>
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm mt-3">
        <div class="card-body">
            <h6 class="form-section-title mb-3">Kết quả trích xuất</h6>
            <div class="table-responsive">
                <table id="rawDataTable" class="table table-sm table-striped mb-0" style="width:100%">
                    <thead>
                        <tr>
                            <th>Tên công ty</th>
                            <th>Mã số thuế</th>
                            <th>Người đại diện</th>
                            <th>Tên tỉnh</th>
                            <th>Điện thoại</th>
                            <th>Địa chỉ</th>
                            <th>Link chi tiết</th>
                        </tr>
                    </thead>
                </table>
            </div>
        </div>
    </div>
    <script>
        $(function () {
            var table = $('#rawDataTable').DataTable({
                processing: true,
                serverSide: true,
                searching: false,
                ajax: {
                    url: 'RawDataApi.aspx',
                    type: 'POST',
                    data: function (d) {
                        d.keyword = $('#<%=txtResultFilter.ClientID%>').val();
                    },
                    dataSrc: function (json) {
                        if (json.error) {
                            alert('Loi tai du lieu: ' + json.error);
                            return [];
                        }
                        return json.data;
                    }
                },
                pageLength: 50,
                lengthMenu: [[20, 50, 100, 200], [20, 50, 100, 200]],
                columns: [
                    { data: 'CompanyName' },
                    { data: 'TaxCode', className: 'text-nowrap' },
                    { data: 'Representative' },
                    { data: 'ProvinceFromAddress' },
                    { data: 'Phone', className: 'text-nowrap' },
                    { data: 'Address' },
                    { data: 'DetailUrl', orderable: false, render: function (data) {
                        if (!data) return '';
                        var safe = $('<div/>').text(data).html();
                        return '<a href="' + safe + '" target="_blank" rel="noopener noreferrer">Xem</a>';
                    }}
                ]
            });

            $('#btnApplyFilter').on('click', function () {
                table.ajax.reload();
            });
            $('#btnClearFilter').on('click', function () {
                $('#<%=txtResultFilter.ClientID%>').val('');
                table.ajax.reload();
            });
        });
    </script>
</asp:Content>
