<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Agents.aspx.cs" Inherits="Agents" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Đại lý & CTV</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .tag-type {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 700;
            font-size: 12px;
            border: 1px solid transparent;
        }
        .tag-company { background: #e0f2fe; color: #075985; border-color: #bae6fd; }
        .tag-collab { background: #fef3c7; color: #92400e; border-color: #fde68a; }
        .tag-status {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 999px;
            font-weight: 700;
            font-size: 12px;
            border: 1px solid transparent;
        }
        .tag-active { background: #dcfce7; color: #166534; border-color: #bbf7d0; }
        .tag-inactive { background: #fee2e2; color: #991b1b; border-color: #fecaca; }
        #agentsTable th, #agentsTable td { white-space: nowrap; }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card shadow-sm">
        <div class="card-body">
            <div class="d-flex flex-column flex-lg-row justify-content-between align-items-lg-center mb-3">
                <div>
                    <h2 class="h4 mb-1 text-primary">Danh sách Đại lý & CTV</h2>
                    <div class="text-muted">Quản lý công ty đại lý và cộng tác viên bán hàng</div>
                </div>
            </div>
            <div class="mb-3">
                <div class="row g-2 align-items-end">
                    <div class="col-md-3 col-lg-2">
                        <label class="form-label fw-semibold">Tên đại lý</label>
                        <asp:TextBox ID="txtName" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3 col-lg-2">
                        <label class="form-label fw-semibold">Điện thoại</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                    </div>
                    <div class="col-md-3 col-lg-2">
                        <label class="form-label fw-semibold">Loại</label>
                        <asp:DropDownList ID="ddlType" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Tất cả" Value="" />
                            <asp:ListItem Text="Công ty" Value="COMPANY" />
                            <asp:ListItem Text="CTV" Value="COLLAB" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-3 col-lg-2">
                        <label class="form-label fw-semibold">Trạng thái</label>
                        <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Tất cả" Value="" />
                            <asp:ListItem Text="Hoạt động" Value="ACTIVE" />
                            <asp:ListItem Text="Tạm dừng" Value="INACTIVE" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-3 col-lg-2">
                        <label class="form-label fw-semibold">Tỉnh</label>
                        <select id="ddlProvince" class="form-select">
                            <option value="">Tất cả</option>
                        </select>
                    </div>
                    <div class="col-md-3 col-lg-2 d-flex gap-2">
                        <asp:Button ID="btnFilter" runat="server" Text="Lọc" CssClass="btn btn-primary" />
                        <asp:Button ID="btnReset" runat="server" Text="Xóa lọc" CssClass="btn btn-outline-secondary" CausesValidation="false" />
                        <asp:Button ID="btnAddAgent" runat="server" Text="+" CssClass="btn btn-success fw-bold px-3" CausesValidation="false" />
                    </div>
                </div>
            </div>
            <div class="table-responsive">
                <table id="agentsTable" class="table table-striped table-hover align-middle mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>Mã</th>
                            <th>Tên đại lý</th>
                            <th>Loại</th>
                            <th>Điện thoại</th>
                            <th>Tỉnh</th>
                            <th>Người đại diện</th>
                            <th>Trạng thái</th>
                        </tr>
                    </thead>
                </table>
            </div>
        </div>
    </div>

    <div class="modal fade" id="agentCreateModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Thêm đại lý</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row g-3">
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Tên đại lý</label>
                            <input type="text" id="agentName" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Loại</label>
                            <select id="agentType" class="form-select">
                                <option value="COMPANY">Công ty</option>
                                <option value="COLLAB">CTV</option>
                            </select>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Trạng thái</label>
                            <select id="agentStatus" class="form-select">
                                <option value="ACTIVE">Hoạt động</option>
                                <option value="INACTIVE">Tạm dừng</option>
                            </select>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Mã</label>
                            <input type="text" id="agentCode" class="form-control" placeholder="Tự động tạo AGxxxxxx" readonly />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Điện thoại</label>
                            <input type="text" id="agentPhone" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Email</label>
                            <input type="text" id="agentEmail" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Mã số thuế</label>
                            <input type="text" id="agentTaxCode" class="form-control" />
                        </div>
                        <div class="col-md-8">
                            <label class="form-label fw-semibold">Địa chỉ thuế</label>
                            <input type="text" id="agentTaxAddress" class="form-control" />
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Tỉnh/Thành</label>
                            <select id="agentProvince" class="form-select">
                                <option value="">-- Chọn tỉnh --</option>
                            </select>
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Phường/Xã</label>
                            <select id="agentWard" class="form-select">
                                <option value="">-- Chọn phường --</option>
                            </select>
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Số nhà</label>
                            <input type="text" id="agentHouseNo" class="form-control" />
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Đường</label>
                            <input type="text" id="agentStreet" class="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">Công ty chủ quản (cho CTV)</label>
                            <select id="agentParent" class="form-select">
                                <option value="">-- Không chọn --</option>
                            </select>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">% hoa hồng</label>
                            <input type="number" id="agentCommission" class="form-control" min="0" step="0.01" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">Người đại diện</label>
                            <input type="text" id="agentRepName" class="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">SĐT đại diện</label>
                            <input type="text" id="agentRepPhone" class="form-control" />
                        </div>
                        <div class="col-md-12">
                            <label class="form-label fw-semibold">Ghi chú</label>
                            <textarea id="agentNote" class="form-control" rows="2"></textarea>
                        </div>
                    </div>
                    <div id="agentCreateError" class="text-danger mt-2" style="display:none;"></div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Đóng</button>
                    <button type="button" class="btn btn-success" id="btnSaveAgent">Lưu</button>
                </div>
            </div>
        </div>
    </div>

    <script>
        $(function () {
            var table = $('#agentsTable').DataTable({
                processing: true,
                serverSide: true,
                searching: false,
                ajax: {
                    url: 'AgentsApi.aspx',
                    type: 'POST',
                    data: function (d) {
                        d.name = $('#<%=txtName.ClientID%>').val();
                        d.phone = $('#<%=txtPhone.ClientID%>').val();
                        d.type = $('#<%=ddlType.ClientID%>').val();
                        d.status = $('#<%=ddlStatus.ClientID%>').val();
                        d.provinceId = $('#ddlProvince').val();
                    }
                },
                pageLength: 50,
                columns: [
                    { data: 'Code' },
                    { data: 'Name', render: function (data, type, row) {
                        if (!data) return '';
                        var safe = $('<div/>').text(data).html();
                        return '<a href="AgentDetail.aspx?id=' + row.Id + '">' + safe + '</a>';
                    }},
                    { data: 'AgentType', render: function (data) {
                        if (!data) return '';
                        var label = data === 'COLLAB' ? 'CTV' : 'Công ty';
                        var cls = data === 'COLLAB' ? 'tag-type tag-collab' : 'tag-type tag-company';
                        return '<span class="' + cls + '">' + label + '</span>';
                    }},
                    { data: 'Phone' },
                    { data: 'ProvinceName' },
                    { data: 'RepresentativeName' },
                    { data: 'Status', render: function (data) {
                        if (!data) return '';
                        var key = String(data).toUpperCase();
                        var label = key === 'INACTIVE' ? 'Tạm dừng' : 'Hoạt động';
                        var cls = key === 'INACTIVE' ? 'tag-status tag-inactive' : 'tag-status tag-active';
                        return '<span class="' + cls + '">' + label + '</span>';
                    }}
                ]
            });

            $('#<%=btnFilter.ClientID%>, #<%=btnReset.ClientID%>').attr('type', 'button');
            $('#<%=btnFilter.ClientID%>').on('click', function () { table.ajax.reload(); });
            $('#<%=btnReset.ClientID%>').on('click', function () {
                $('#<%=txtName.ClientID%>').val('');
                $('#<%=txtPhone.ClientID%>').val('');
                $('#<%=ddlType.ClientID%>').val('');
                $('#<%=ddlStatus.ClientID%>').val('');
                $('#ddlProvince').val('');
                table.ajax.reload();
            });

            $('#<%=btnAddAgent.ClientID%>').attr('type', 'button');
            $('#<%=btnAddAgent.ClientID%>').on('click', function () {
                $('#agentCreateError').hide().text('');
                loadCompanies();
                loadProvinces('#agentProvince', '#agentWard');
                var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('agentCreateModal'));
                modal.show();
            });

            loadProvincesFilter();

            $('#btnSaveAgent').on('click', function () {
                var payload = {
                    name: $('#agentName').val(),
                    type: $('#agentType').val(),
                    status: $('#agentStatus').val(),
                    phone: $('#agentPhone').val(),
                    email: $('#agentEmail').val(),
                    taxCode: $('#agentTaxCode').val(),
                    taxAddress: $('#agentTaxAddress').val(),
                    provinceId: $('#agentProvince').val(),
                    wardId: $('#agentWard').val(),
                    houseNo: $('#agentHouseNo').val(),
                    street: $('#agentStreet').val(),
                    fullAddress: buildAddressText('#agentHouseNo', '#agentStreet', '#agentWard', '#agentProvince'),
                    parentId: $('#agentParent').val(),
                    commissionRate: $('#agentCommission').val(),
                    representativeName: $('#agentRepName').val(),
                    representativePhone: $('#agentRepPhone').val(),
                    contractNo: '',
                    contractDate: '',
                    contractExpiry: '',
                    licenseNo: '',
                    licenseDate: '',
                    licenseExpiry: '',
                    note: $('#agentNote').val()
                };

                $.ajax({
                    url: 'AgentsCreateApi.aspx',
                    type: 'POST',
                    dataType: 'json',
                    data: payload,
                    success: function (resp) {
                        if (resp && resp.error) {
                            $('#agentCreateError').text(resp.error).show();
                            return;
                        }
                        bootstrap.Modal.getOrCreateInstance(document.getElementById('agentCreateModal')).hide();
                        table.ajax.reload();
                    },
                    error: function () {
                        $('#agentCreateError').text('Lỗi lưu đại lý').show();
                    }
                });
            });
        });

        function loadCompanies() {
            $.ajax({
                url: 'AgentsCompaniesApi.aspx',
                type: 'GET',
                dataType: 'json',
                success: function (resp) {
                    var $select = $('#agentParent');
                    $select.empty();
                    $select.append('<option value=\"\">-- Không chọn --</option>');
                    if (resp && resp.data && resp.data.length) {
                        $.each(resp.data, function (i, o) {
                            $select.append('<option value=\"' + o.Id + '\">' + o.Name + '</option>');
                        });
                    }
                }
            });
        }

        function loadProvinces(provinceSelector, wardSelector) {
            $.ajax({
                url: 'ProvincesApi.aspx',
                type: 'GET',
                dataType: 'json',
                success: function (resp) {
                    var $province = $(provinceSelector);
                    $province.empty();
                    $province.append('<option value=\"\">-- Chọn tỉnh --</option>');
                    if (resp && resp.data && resp.data.length) {
                        $.each(resp.data, function (i, p) {
                            $province.append('<option value=\"' + p.Id + '\">' + p.Name + '</option>');
                        });
                    }
                    loadWards(wardSelector, $province.val());
                }
            });

            $(document).off('change', provinceSelector).on('change', provinceSelector, function () {
                loadWards(wardSelector, $(this).val());
            });
        }

        function loadProvincesFilter() {
            $.ajax({
                url: 'ProvincesApi.aspx',
                type: 'GET',
                dataType: 'json',
                success: function (resp) {
                    var $province = $('#ddlProvince');
                    $province.empty();
                    $province.append('<option value="">Tất cả</option>');
                    if (resp && resp.data && resp.data.length) {
                        $.each(resp.data, function (i, p) {
                            $province.append('<option value="' + p.Id + '">' + p.Name + '</option>');
                        });
                    }
                }
            });
        }

        function loadWards(wardSelector, provinceId) {
            $.ajax({
                url: 'WardsApi.aspx',
                type: 'GET',
                dataType: 'json',
                data: { provinceId: provinceId || '' },
                success: function (resp) {
                    var $ward = $(wardSelector);
                    $ward.empty();
                    $ward.append('<option value=\"\">-- Chọn phường --</option>');
                    if (resp && resp.data && resp.data.length) {
                        $.each(resp.data, function (i, w) {
                            $ward.append('<option value=\"' + w.Id + '\">' + w.Name + '</option>');
                        });
                    }
                }
            });
        }

        function buildAddressText(houseSelector, streetSelector, wardSelector, provinceSelector) {
            var parts = [];
            var house = $(houseSelector).val();
            var street = $(streetSelector).val();
            var ward = $(wardSelector + ' option:selected').text();
            var province = $(provinceSelector + ' option:selected').text();
            if (house) parts.push(house);
            if (street) parts.push(street);
            if (ward && ward.indexOf('Chọn') === -1) parts.push(ward);
            if (province && province.indexOf('Chọn') === -1) parts.push(province);
            return parts.join(', ');
        }

        // upload hồ sơ đã bỏ khỏi form thêm đại lý
    </script>
</asp:Content>
