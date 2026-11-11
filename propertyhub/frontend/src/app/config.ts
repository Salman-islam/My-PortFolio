export interface Field {
  key: string;
  label: string;
  type: string;
  source?: string;
  display?: string;
  options?: any[];
  min?: number;
  max?: number;
  step?: string;
  minlength?: number;
  createOnly?: boolean;
  optional?: boolean;
}
export interface Page {
  key: string;
  title: string;
  columns: { key: string; label: string }[];
  fields: Field[];
  roles?: string[];
  createRoles?: string[];
  editRoles?: string[];
  deleteRoles?: string[];
  filter?: string;
}
export const config: {
  slug: string;
  title: string;
  tag: string;
  color: string;
  description: string;
  primary: string;
  pages: Page[];
} = {
  slug: "propertyhub",
  title: "PropertyHub",
  tag: "Properties & reservations",
  color: "#bd623b",
  description: "Connected communities. Effortless reservations.",
  primary: "bookings",
  pages: [
    {
      key: "properties",
      title: "Properties",
      columns: [
        {
          key: "name",
          label: "Property",
        },
        {
          key: "address",
          label: "Address",
        },
      ],
      fields: [
        {
          key: "name",
          label: "Name",
          type: "text",
        },
        {
          key: "address",
          label: "Address",
          type: "text",
        },
      ],
      roles: ["Admin", "Staff"],
      createRoles: ["Admin", "Staff"],
      editRoles: ["Admin", "Staff"],
      deleteRoles: ["Admin"],
    },
    {
      key: "rooms",
      title: "Rooms",
      columns: [
        {
          key: "property",
          label: "Property",
        },
        {
          key: "number",
          label: "Room",
        },
        {
          key: "type",
          label: "Type",
        },
        {
          key: "occupied",
          label: "Occupied",
        },
      ],
      fields: [
        {
          key: "propertyId",
          label: "Property",
          type: "select",
          source: "properties",
          display: "name",
        },
        {
          key: "number",
          label: "Room number",
          type: "text",
        },
        {
          key: "type",
          label: "Room type",
          type: "text",
        },
      ],
      roles: ["Admin", "Staff"],
      createRoles: ["Admin", "Staff"],
      editRoles: ["Admin", "Staff"],
      deleteRoles: ["Admin"],
    },
    {
      key: "residents",
      title: "Residents",
      columns: [
        {
          key: "name",
          label: "Resident",
        },
        {
          key: "email",
          label: "Email",
        },
        {
          key: "property",
          label: "Property",
        },
        {
          key: "room",
          label: "Room",
        },
      ],
      fields: [
        {
          key: "name",
          label: "Name",
          type: "text",
        },
        {
          key: "email",
          label: "Email",
          type: "email",
        },
        {
          key: "password",
          label: "Initial password",
          type: "password",
          createOnly: true,
          minlength: 12,
        },
        {
          key: "propertyId",
          label: "Property",
          type: "select",
          source: "properties",
          display: "name",
        },
        {
          key: "roomId",
          label: "Room (optional)",
          type: "select",
          source: "rooms",
          display: "number",
          optional: true,
        },
      ],
      roles: ["Admin", "Staff"],
      createRoles: ["Admin", "Staff"],
      editRoles: ["Admin", "Staff"],
    },
    {
      key: "amenities",
      title: "Amenities",
      columns: [
        {
          key: "name",
          label: "Amenity",
        },
        {
          key: "property",
          label: "Property",
        },
        {
          key: "capacity",
          label: "Capacity",
        },
        {
          key: "active",
          label: "Active",
        },
      ],
      fields: [
        {
          key: "propertyId",
          label: "Property",
          type: "select",
          source: "properties",
          display: "name",
        },
        {
          key: "name",
          label: "Amenity name",
          type: "text",
        },
        {
          key: "capacity",
          label: "Capacity",
          type: "number",
          min: 1,
          max: 500,
        },
        {
          key: "active",
          label: "Available",
          type: "select",
          options: [true, false],
        },
      ],
      createRoles: ["Admin", "Staff"],
      editRoles: ["Admin", "Staff"],
      deleteRoles: ["Admin"],
    },
    {
      key: "bookings",
      title: "Bookings",
      columns: [
        {
          key: "amenity",
          label: "Amenity",
        },
        {
          key: "resident",
          label: "Resident",
        },
        {
          key: "start",
          label: "Start",
        },
        {
          key: "end",
          label: "End",
        },
        {
          key: "guests",
          label: "Guests",
        },
        {
          key: "status",
          label: "Status",
        },
      ],
      fields: [
        {
          key: "amenityId",
          label: "Amenity",
          type: "select",
          source: "amenities",
          display: "name",
        },
        {
          key: "residentId",
          label: "Resident",
          type: "select",
          source: "residents",
          display: "name",
        },
        {
          key: "start",
          label: "Starts at",
          type: "datetime-local",
        },
        {
          key: "end",
          label: "Ends at",
          type: "datetime-local",
        },
        {
          key: "guests",
          label: "Guest count",
          type: "number",
          min: 1,
          max: 500,
        },
        {
          key: "purpose",
          label: "Purpose",
          type: "textarea",
        },
      ],
      createRoles: ["Admin", "Staff", "Resident"],
      filter: "status",
    },
    {
      key: "announcements",
      title: "Announcements",
      columns: [
        {
          key: "title",
          label: "Title",
        },
        {
          key: "property",
          label: "Property",
        },
        {
          key: "body",
          label: "Message",
        },
        {
          key: "startsAt",
          label: "Published from",
        },
        {
          key: "endsAt",
          label: "Expires",
        },
      ],
      fields: [
        {
          key: "propertyId",
          label: "Property",
          type: "select",
          source: "properties",
          display: "name",
        },
        {
          key: "title",
          label: "Title",
          type: "text",
        },
        {
          key: "body",
          label: "Message",
          type: "textarea",
          max: 4000,
        },
        {
          key: "startsAt",
          label: "Published from",
          type: "datetime-local",
        },
        {
          key: "endsAt",
          label: "Expires",
          type: "datetime-local",
        },
      ],
      createRoles: ["Admin", "Staff"],
      editRoles: ["Admin", "Staff"],
      deleteRoles: ["Admin", "Staff"],
    },
    {
      key: "users",
      title: "User accounts",
      columns: [
        {
          key: "name",
          label: "Name",
        },
        {
          key: "email",
          label: "Email",
        },
        {
          key: "role",
          label: "Role",
        },
        {
          key: "active",
          label: "Active",
        },
      ],
      fields: [
        {
          key: "name",
          label: "Name",
          type: "text",
        },
        {
          key: "email",
          label: "Email",
          type: "email",
        },
        {
          key: "password",
          label: "Initial password",
          type: "password",
          minlength: 12,
        },
        {
          key: "role",
          label: "Role",
          type: "select",
          options: ["Admin", "Staff"],
        },
      ],
      roles: ["Admin"],
      createRoles: ["Admin"],
    },
    {
      key: "audit",
      title: "Audit history",
      columns: [
        {
          key: "actor",
          label: "Actor",
        },
        {
          key: "action",
          label: "Action",
        },
        {
          key: "detail",
          label: "Details",
        },
        {
          key: "at",
          label: "Timestamp",
        },
      ],
      fields: [],
      roles: ["Admin"],
    },
  ],
};
