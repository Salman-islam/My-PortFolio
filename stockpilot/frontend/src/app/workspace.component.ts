import { Component, OnInit, OnDestroy, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { Subscription } from "rxjs";
import { ApiService } from "./api.service";
import { config, Page, Field } from "./config";
@Component({
  selector: "app-workspace",
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: "./workspace.component.html",
})
export class WorkspaceComponent implements OnInit, OnDestroy {
  api = inject(ApiService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  config = config;
  data: any = {};
  key = "dashboard";
  search = "";
  filter = "";
  error = "";
  notice = "";
  loading = false;
  busy = false;
  email = "admin@example.com";
  password = "";
  modal = false;
  form: any = {};
  editId: number | null = null;
  formPage!: Page;
  fields: Field[] = [];
  formTitle = "";
  actionPath = "";
  detail: any = null;
  detailRow: any = null;
  comment = "";
  internal = false;
  availability = "";
  private subscription?: Subscription;
  ngOnInit() {
    document.documentElement.style.setProperty("--accent", config.color);
    this.subscription = this.route.paramMap.subscribe((p) => {
      this.key = p.get("page") || "dashboard";
      this.search = "";
      this.filter = "";
      if (this.api.session.user()) this.refresh();
    });
  }
  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }
  get user() {
    return this.api.session.user();
  }
  get admin() {
    return this.user?.role === "Admin";
  }
  get staff() {
    return this.admin || this.user?.role === "Staff";
  }
  can(roles?: string[]) {
    return !!roles?.includes(this.user?.role || "");
  }
  get nav() {
    return config.pages.filter((p) => !p.roles || this.can(p.roles));
  }
  get page() {
    return config.pages.find((p) => p.key === this.key);
  }
  get tablePage() {
    return this.page || config.pages.find((p) => p.key === config.primary)!;
  }
  get rows(): any[] {
    return this.data[this.tablePage.key] || [];
  }
  get filtered() {
    const q = this.search.trim().toLowerCase();
    return this.rows.filter(
      (x) =>
        (!this.filter ||
          String(x[this.tablePage.filter || "status"]) === this.filter) &&
        this.tablePage.columns
          .map((c) => String(x[c.key] ?? ""))
          .join(" ")
          .toLowerCase()
          .includes(q),
    );
  }
  get filterOptions(): string[] {
    return [
      ...new Set(
        this.rows.map((x) => String(x[this.tablePage.filter || "status"])),
      ),
    ];
  }
  get metrics(): any[] {
    return this.data.summary || [];
  }
  get formFields() {
    return this.fields.filter((f) => !(f.createOnly && this.editId !== null));
  }
  login() {
    if (this.busy) return;
    this.busy = true;
    this.error = "";
    this.api
      .post("auth/login", { email: this.email, password: this.password })
      .subscribe({
        next: (r) => {
          this.api.session.token = r.token;
          this.api.session.user.set(r.user);
          this.password = "";
          this.busy = false;
          this.router.navigate(["/dashboard"]);
          this.refresh();
        },
        error: (e) => {
          this.busy = false;
          this.fail(e);
        },
      });
  }
  logout() {
    this.api.session.clear();
    this.data = {};
    this.modal = false;
    this.detail = null;
    this.notice = "";
    this.error = "";
    this.router.navigate(["/dashboard"]);
  }
  fail(e: any) {
    this.error =
      e.error?.message ||
      e.error?.title ||
      (e.status === 0
        ? "Cannot reach the API. Start the backend, then retry."
        : "Request failed. Refresh and try again.");
  }
  refresh() {
    if (this.page?.roles && !this.can(this.page.roles)) {
      this.router.navigate(["/dashboard"]);
      return;
    }
    this.loading = true;
    this.error = "";
    this.api.get("workspace").subscribe({
      next: (r) => {
        this.data = r;
        this.loading = false;
        if (this.key === "users" || this.key === "audit")
          this.api
            .get(this.key)
            .subscribe({
              next: (x) => (this.data[this.key] = x),
              error: (e) => this.fail(e),
            });
      },
      error: (e) => {
        this.loading = false;
        this.fail(e);
      },
    });
  }
  display(value: any, key: string) {
    if (value === null || value === undefined || value === "") return "—";
    if (typeof value === "boolean") return value ? "Yes" : "No";
    if (["startDate", "day"].includes(key)) return String(value).slice(0, 10);
    if (["start", "end"].includes(key) && config.slug === "peopleops")
      return String(value).slice(0, 10);
    if (
      [
        "start",
        "end",
        "at",
        "createdAt",
        "postedAt",
        "checkIn",
        "checkOut",
        "startsAt",
        "endsAt",
      ].includes(key)
    )
      return new Date(
        String(value).endsWith("Z") ? value : value + "Z",
      ).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
    if (["price", "total", "unitPrice"].includes(key))
      return Number(value).toLocaleString(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      });
    return String(value);
  }
  isBadge(key: string) {
    return [
      "status",
      "role",
      "priority",
      "active",
      "kind",
      "occupied",
    ].includes(key);
  }
  options(field: Field): any[] {
    if (field.options)
      return field.options.map((v) => ({
        id: v,
        name: typeof v === "boolean" ? (v ? "Yes" : "No") : v,
      }));
    let values = this.data[field.source || ""] || [];
    if (field.key === "roomId")
      values = values.filter(
        (x: any) =>
          x.propertyId === this.form.propertyId &&
          (!x.occupied || x.id === this.form.roomId),
      );
    if (field.key === "partyId")
      values = values.filter(
        (x: any) =>
          x.kind === (this.form.kind === "Purchase" ? "Supplier" : "Customer"),
      );
    if (field.key === "residentId") {
      const a = (this.data.amenities || []).find(
        (x: any) => x.id === this.form.amenityId,
      );
      if (a) values = values.filter((x: any) => x.propertyId === a.propertyId);
    }
    if (field.source === "amenities" && this.formPage?.key === "bookings")
      values = values.filter((x: any) => x.active);
    return values.map((x: any) => ({
      id: x.id,
      name: x[field.display || "name"],
    }));
  }
  open(page: Page, row?: any) {
    this.error = "";
    this.notice = "";
    this.availability = "";
    this.actionPath = "";
    this.formPage = page;
    this.fields = page.fields;
    this.editId = row?.id ?? null;
    this.form = row ? structuredClone(row) : {};
    this.formTitle = (row ? "Edit " : "Add ") + page.title.toLowerCase();
    for (const f of this.fields) {
      if (this.form[f.key] == null) {
        if (f.type === "number") this.form[f.key] = f.min ?? 0;
        else if (f.options) this.form[f.key] = f.options[0];
        else this.form[f.key] = f.optional ? null : "";
      }
      if (f.type === "date" && this.form[f.key])
        this.form[f.key] = String(this.form[f.key]).slice(0, 10);
      if (f.type === "datetime-local" && this.form[f.key]) {
        const d = new Date(
          String(this.form[f.key]).endsWith("Z")
            ? this.form[f.key]
            : this.form[f.key] + "Z",
        );
        this.form[f.key] = new Date(d.getTime() - d.getTimezoneOffset() * 60000)
          .toISOString()
          .slice(0, 16);
      }
    }
    if (page.key === "orders" && !row)
      this.form.lines = [{ productId: null, quantity: 1, unitPrice: 0 }];
    this.modal = true;
  }
  formAction(
    title: string,
    path: string,
    fields: Field[],
    row: any,
    defaults: any = {},
  ) {
    this.open({ key: "action", title, columns: [], fields });
    this.formTitle = title;
    this.actionPath = path;
    this.form = { ...this.form, ...defaults, version: row.version };
  }
  payload() {
    const body = { ...this.form };
    for (const f of this.fields) {
      if (f.type === "datetime-local" && body[f.key])
        body[f.key] = new Date(body[f.key]).toISOString();
      if (f.createOnly && this.editId !== null) body[f.key] = "";
    }
    return body;
  }
  save() {
    if (this.busy) return;
    this.busy = true;
    this.error = "";
    const path =
      this.actionPath ||
      this.formPage.key + (this.editId !== null ? "/" + this.editId : "");
    const req =
      this.actionPath || this.editId === null
        ? this.api.post(path, this.payload())
        : this.api.put(path, this.payload());
    req.subscribe({
      next: () => {
        this.busy = false;
        this.modal = false;
        this.notice = "Changes saved successfully.";
        this.refresh();
      },
      error: (e) => {
        this.busy = false;
        this.fail(e);
      },
    });
  }
  execute(path: string, body: any, prompt: string) {
    if (this.busy || !confirm(prompt)) return;
    this.busy = true;
    this.error = "";
    this.api.post(path, body).subscribe({
      next: () => {
        this.busy = false;
        this.notice = "Action completed successfully.";
        this.refresh();
      },
      error: (e) => {
        this.busy = false;
        this.fail(e);
      },
    });
  }
  remove(page: Page, row: any) {
    if (!confirm("Delete this record? Referenced records cannot be deleted."))
      return;
    this.api.delete(page.key + "/" + row.id).subscribe({
      next: () => {
        this.notice = "Record deleted.";
        this.refresh();
      },
      error: (e) => this.fail(e),
    });
  }
  canEdit(page: Page, row: any) {
    if (!this.can(page.editRoles)) return false;
    if (page.key === "orders") return row.status === "Draft";
    if (page.key === "tickets")
      return (
        row.status === "Open" &&
        (this.admin ||
          row.requesterId === this.user?.id ||
          row.assignedToId === this.user?.id)
      );
    return true;
  }
  actions(page: Page, row: any): string[] {
    switch (page.key) {
      case "leaves":
        return [
          ...(this.admin && row.status === "Pending"
            ? ["Approve", "Reject"]
            : []),
          ...(["Pending", "Approved"].includes(row.status) &&
          row.start.slice(0, 10) >= new Date().toISOString().slice(0, 10)
            ? ["Cancel"]
            : []),
        ];
      case "products":
        return this.admin ? ["Adjust stock"] : [];
      case "orders":
        return [
          "Details",
          ...(row.status === "Draft" ? ["Post order", "Cancel"] : []),
        ];
      case "tickets":
        return [
          "Conversation",
          ...(this.staff &&
          ["Open", "In progress"].includes(row.status) &&
          (this.admin ||
            !row.assignedToId ||
            row.assignedToId === this.user?.id)
            ? ["Assign"]
            : []),
          ...(this.staff && (this.admin || row.assignedToId === this.user?.id)
            ? row.status === "Open"
              ? ["Start"]
              : row.status === "In progress"
                ? ["Resolve"]
                : row.status === "Resolved"
                  ? ["Close", "Reopen"]
                  : []
            : !this.staff && row.status === "Resolved"
              ? ["Reopen"]
              : []),
        ];
      case "bookings":
        return row.status === "Confirmed" &&
          new Date(
            String(row.start).endsWith("Z") ? row.start : row.start + "Z",
          ) > new Date()
          ? ["Cancel"]
          : [];
      case "users":
        return row.id === this.user?.id
          ? []
          : [row.active ? "Deactivate" : "Activate"];
      default:
        return [];
    }
  }
  action(label: string, page: Page, row: any) {
    const path = page.key + "/" + row.id;
    if (label === "Approve" || label === "Reject") {
      this.formAction(
        label + " leave",
        path + "/decision",
        [{ key: "note", label: "Decision note", type: "textarea" }],
        row,
        { status: label === "Approve" ? "Approved" : "Rejected" },
      );
      return;
    }
    if (label === "Adjust stock") {
      this.formAction(
        "Adjust stock · " + row.name,
        path + "/adjust",
        [
          {
            key: "delta",
            label: "Quantity change (+ receive, − remove)",
            type: "number",
            min: -1000000,
            max: 1000000,
          },
          { key: "reason", label: "Reason", type: "textarea" },
        ],
        row,
      );
      return;
    }
    if (label === "Assign") {
      this.formAction(
        "Assign support agent",
        path + "/assign",
        [
          {
            key: "assignedToId",
            label: "Support agent",
            type: "select",
            source: "agents",
          },
        ],
        row,
        { assignedToId: this.staff && !this.admin ? this.user?.id : null },
      );
      return;
    }
    if (["Start", "Resolve", "Close", "Reopen"].includes(label)) {
      this.execute(
        path + "/status",
        {
          version: row.version,
          status: (
            {
              Start: "In progress",
              Resolve: "Resolved",
              Close: "Closed",
              Reopen: "Open",
            } as any
          )[label],
        },
        label + " this ticket?",
      );
      return;
    }
    if (label === "Post order") {
      this.execute(
        path + "/post",
        { version: row.version },
        "Post this order? Stock will change and this order becomes read-only.",
      );
      return;
    }
    if (label === "Cancel") {
      this.execute(
        path + "/cancel",
        { version: row.version },
        "Cancel this record?",
      );
      return;
    }
    if (label === "Activate" || label === "Deactivate") {
      if (!confirm(label + " this account?")) return;
      this.api
        .put(path + "/active", { active: !row.active })
        .subscribe({ next: () => this.refresh(), error: (e) => this.fail(e) });
      return;
    }
    if (label === "Conversation") {
      this.detailRow = row;
      this.comment = "";
      this.internal = false;
      this.api
        .get(path)
        .subscribe({
          next: (r) => (this.detail = r),
          error: (e) => this.fail(e),
        });
      return;
    }
    if (label === "Details") {
      this.detailRow = row;
      this.detail = { invoice: true };
    }
  }
  addComment() {
    if (!this.comment.trim() || this.busy) return;
    this.busy = true;
    this.api
      .post("tickets/" + this.detailRow.id + "/comments", {
        body: this.comment,
        internal: this.internal,
      })
      .subscribe({
        next: () => {
          this.busy = false;
          this.action("Conversation", this.tablePage, this.detailRow);
        },
        error: (e) => {
          this.busy = false;
          this.fail(e);
        },
      });
  }
  attendance(action: string) {
    this.execute(
      "attendance/" + action,
      {},
      action === "check-in" ? "Check in now?" : "Check out now?",
    );
  }
  lineProduct(line: any) {
    const p = (this.data.products || []).find(
      (x: any) => x.id === line.productId,
    );
    if (p) line.unitPrice = p.price;
  }
  get orderTotal() {
    return (this.form.lines || []).reduce(
      (sum: number, l: any) =>
        sum + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0),
      0,
    );
  }
  checkAvailability() {
    const body = this.payload();
    if (!body.amenityId || !body.start || !body.end) {
      this.availability = "Select an amenity and both times first.";
      return;
    }
    this.api
      .get(
        "availability?amenityId=" +
          body.amenityId +
          "&start=" +
          encodeURIComponent(body.start) +
          "&end=" +
          encodeURIComponent(body.end),
      )
      .subscribe({
        next: (r) =>
          (this.availability = r.available
            ? "This slot is available. Availability is checked again when you save."
            : "This slot is already booked. Choose another time."),
        error: (e) => this.fail(e),
      });
  }
  print() {
    window.print();
  }
  exportCsv() {
    const page = this.tablePage;
    const csv = [
      page.columns.map((c) => c.label),
      ...this.filtered.map((r) =>
        page.columns.map((c) => this.display(r[c.key], c.key)),
      ),
    ]
      .map((row) =>
        row
          .map(
            (v) =>
              '"' +
              String(/^[=+\-@]/.test(v) ? "'" + v : v).replaceAll('"', '""') +
              '"',
          )
          .join(","),
      )
      .join("\r\n");
    const url = URL.createObjectURL(
      new Blob([csv], { type: "text/csv;charset=utf-8;" }),
    );
    const a = document.createElement("a");
    a.href = url;
    a.download = config.slug + "-" + page.key + ".csv";
    a.click();
    URL.revokeObjectURL(url);
  }
}
