open! Core
open! Bonsai_web.Cont
open Bonsai.Let_syntax

module Js = Js_of_ocaml.Js
module Unsafe = Js_of_ocaml.Js.Unsafe

type game_action =
  { action_id : string
  ; label : string
  ; kind : string
  ; enabled : bool
  ; selected : bool
  }

type input_spec =
  { kind : string
  ; label : string
  ; placeholder : string
  ; required : bool
  ; min : int option
  ; max : int option
  ; max_length : int option
  ; action_id : string
  }

type hud =
  { date : string
  ; turns : int
  ; balance : float
  ; vehicle_name : string
  ; vehicle_status : string
  ; location_name : string
  ; location_status : string
  ; weather : string
  ; health : string
  ; pace : string
  ; rations : string
  ; cargo_weight : int
  ; cargo_capacity : int
  }

type party_member =
  { name : string
  ; is_leader : bool
  ; profession : string
  ; health : string
  }

type inventory_item =
  { item_id : string
  ; name : string
  ; quantity : int
  ; unit : string
  ; unit_weight : int
  ; total_weight : int
  }

type progress =
  { miles_traveled : int
  ; total_miles : int
  ; miles_to_next_location : int
  ; next_location : string
  ; activity_label : string
  ; activity_current : int option
  ; activity_total : int option
  }

type store_row =
  { item_id : string
  ; name : string
  ; quantity : int
  ; unit_price : float
  ; total_price : float
  ; min_quantity : int
  ; max_quantity : int
  ; selected : bool
  ; decrease_action_id : string
  ; increase_action_id : string
  ; set_action_id : string
  }

type store =
  { location : string
  ; balance : float
  ; pending_total : float
  ; cargo_weight : int
  ; cargo_capacity : int
  ; rows : store_row list
  }

type screen =
  { kind : string
  ; id : string
  ; title : string
  ; description : string
  ; input : input_spec option
  ; actions : game_action list
  }

type game_state =
  { revision : int
  ; running : bool
  ; hud : hud
  ; party : party_member list
  ; inventory : inventory_item list
  ; progress : progress
  ; store : store option
  ; screen : screen
  ; response_message : string
  ; action_accepted : bool option
  }

type connection =
  | Loading
  | Failed of string
  | Ready of game_state

type draft =
  { screen_id : string
  ; value : string
  }

let connection_var = Bonsai.Expert.Var.create Loading
let busy_var = Bonsai.Expert.Var.create false
let reconnecting_var = Bonsai.Expert.Var.create false
let draft_var = Bonsai.Expert.Var.create { screen_id = ""; value = "" }
let latest_revision = ref (-1)
let latest_screen_id = ref ""
let latest_focus_key = ref ""
let action_pending = ref false
let last_state : game_state option ref = ref None

let string_field object_ name =
  let value : Js.js_string Js.t = Unsafe.get object_ name in
  Js.to_string value
;;

let bool_field object_ name =
  let value : bool Js.t = Unsafe.get object_ name in
  Js.to_bool value
;;

let int_field (object_ : Unsafe.any) name : int = Unsafe.get object_ name
let float_field (object_ : Unsafe.any) name : float = Unsafe.get object_ name

let nullable_field (object_ : Unsafe.any) name decode =
  let value : Unsafe.any Js.Opt.t = Unsafe.get object_ name in
  Js.Opt.to_option value |> Option.map ~f:decode
;;

let nullable_int object_ name =
  let value : Unsafe.any Js.Opt.t = Unsafe.get object_ name in
  if Js.Opt.test value then Some (int_field object_ name) else None
;;

let nullable_bool object_ name =
  let value : Unsafe.any Js.Opt.t = Unsafe.get object_ name in
  if Js.Opt.test value then Some (bool_field object_ name) else None
;;

let list_field (object_ : Unsafe.any) name decode =
  let values : Unsafe.any Js.js_array Js.t = Unsafe.get object_ name in
  Js.to_array values |> Array.to_list |> List.map ~f:decode
;;

let decode_action object_ =
  { action_id = string_field object_ "actionId"
  ; label = string_field object_ "label"
  ; kind = string_field object_ "kind"
  ; enabled = bool_field object_ "enabled"
  ; selected = bool_field object_ "selected"
  }
;;

let decode_input object_ =
  { kind = string_field object_ "kind"
  ; label = string_field object_ "label"
  ; placeholder = string_field object_ "placeholder"
  ; required = bool_field object_ "required"
  ; min = nullable_int object_ "min"
  ; max = nullable_int object_ "max"
  ; max_length = nullable_int object_ "maxLength"
  ; action_id = string_field object_ "actionId"
  }
;;

let decode_hud object_ =
  { date = string_field object_ "date"
  ; turns = int_field object_ "turns"
  ; balance = float_field object_ "balance"
  ; vehicle_name = string_field object_ "vehicleName"
  ; vehicle_status = string_field object_ "vehicleStatus"
  ; location_name = string_field object_ "locationName"
  ; location_status = string_field object_ "locationStatus"
  ; weather = string_field object_ "weather"
  ; health = string_field object_ "health"
  ; pace = string_field object_ "pace"
  ; rations = string_field object_ "rations"
  ; cargo_weight = int_field object_ "cargoWeight"
  ; cargo_capacity = int_field object_ "cargoCapacity"
  }
;;

let decode_party_member object_ =
  { name = string_field object_ "name"
  ; is_leader = bool_field object_ "isLeader"
  ; profession = string_field object_ "profession"
  ; health = string_field object_ "health"
  }
;;

let decode_inventory_item object_ =
  { item_id = string_field object_ "itemId"
  ; name = string_field object_ "name"
  ; quantity = int_field object_ "quantity"
  ; unit = string_field object_ "unit"
  ; unit_weight = int_field object_ "unitWeight"
  ; total_weight = int_field object_ "totalWeight"
  }
;;

let decode_progress object_ =
  { miles_traveled = int_field object_ "milesTraveled"
  ; total_miles = int_field object_ "totalMiles"
  ; miles_to_next_location = int_field object_ "milesToNextLocation"
  ; next_location = string_field object_ "nextLocation"
  ; activity_label = string_field object_ "activityLabel"
  ; activity_current = nullable_int object_ "activityCurrent"
  ; activity_total = nullable_int object_ "activityTotal"
  }
;;

let decode_store_row object_ =
  { item_id = string_field object_ "itemId"
  ; name = string_field object_ "name"
  ; quantity = int_field object_ "quantity"
  ; unit_price = float_field object_ "unitPrice"
  ; total_price = float_field object_ "totalPrice"
  ; min_quantity = int_field object_ "minQuantity"
  ; max_quantity = int_field object_ "maxQuantity"
  ; selected = bool_field object_ "selected"
  ; decrease_action_id = string_field object_ "decreaseActionId"
  ; increase_action_id = string_field object_ "increaseActionId"
  ; set_action_id = string_field object_ "setActionId"
  }
;;

let decode_store object_ =
  { location = string_field object_ "location"
  ; balance = float_field object_ "balance"
  ; pending_total = float_field object_ "pendingTotal"
  ; cargo_weight = int_field object_ "cargoWeight"
  ; cargo_capacity = int_field object_ "cargoCapacity"
  ; rows = list_field object_ "rows" decode_store_row
  }
;;

let decode_screen object_ =
  { kind = string_field object_ "kind"
  ; id = string_field object_ "id"
  ; title = string_field object_ "title"
  ; description = string_field object_ "description"
  ; input = nullable_field object_ "input" decode_input
  ; actions = list_field object_ "actions" decode_action
  }
;;

let decode_state json =
  let object_ : Unsafe.any = Js_of_ocaml.Json.unsafe_input json in
  { revision = int_field object_ "revision"
  ; running = bool_field object_ "running"
  ; hud = decode_hud (Unsafe.get object_ "hud")
  ; party = list_field object_ "party" decode_party_member
  ; inventory = list_field object_ "inventory" decode_inventory_item
  ; progress = decode_progress (Unsafe.get object_ "progress")
  ; store = nullable_field object_ "store" decode_store
  ; screen = decode_screen (Unsafe.get object_ "screen")
  ; response_message = string_field object_ "responseMessage"
  ; action_accepted = nullable_bool object_ "actionAccepted"
  }
;;

let api : Unsafe.any = Unsafe.get Unsafe.global "OregonTrailApi"

let focus_screen () =
  ignore (Unsafe.meth_call api "focusScreen" [||] : Unsafe.any)
;;

let accept_state state =
  if state.revision >= !latest_revision
  then (
    let screen_changed = not (String.equal state.screen.id !latest_screen_id) in
    let focus_key =
      String.concat
        ~sep:"\000"
        [ state.screen.kind; state.screen.id; state.screen.title; state.screen.description ]
    in
    let focus_changed = not (String.equal focus_key !latest_focus_key) in
    if state.revision > !latest_revision
       || screen_changed
       || not (String.is_empty state.response_message)
    then (
      latest_revision := state.revision;
      latest_screen_id := state.screen.id;
      latest_focus_key := focus_key;
      if screen_changed
      then Bonsai.Expert.Var.set draft_var { screen_id = state.screen.id; value = "" };
      last_state := Some state;
      Bonsai.Expert.Var.set connection_var (Ready state);
      if focus_changed then focus_screen ()))
;;

let show_error message =
  match !last_state with
  | None -> Bonsai.Expert.Var.set connection_var (Failed message)
  | Some state ->
    let state = { state with response_message = message } in
    last_state := Some state;
    Bonsai.Expert.Var.set connection_var (Ready state)
;;

let on_state_success =
  Js.wrap_callback (fun json ->
    try
      let state = decode_state json in
      Bonsai.Expert.Var.set reconnecting_var false;
      accept_state state
    with
    | exn ->
      Bonsai.Expert.Var.set reconnecting_var true;
      show_error ("The game server returned invalid state: " ^ Exn.to_string exn))
;;

let on_state_failure =
  Js.wrap_callback (fun message ->
    Bonsai.Expert.Var.set reconnecting_var true;
    match !last_state with
    | Some _ -> ()
    | None -> show_error (Js.to_string message))
;;

let on_action_success =
  Js.wrap_callback (fun json ->
    action_pending := false;
    Bonsai.Expert.Var.set busy_var false;
    try
      let state = decode_state json in
      accept_state state;
      if Option.value state.action_accepted ~default:false
      then Bonsai.Expert.Var.set draft_var { screen_id = state.screen.id; value = "" }
    with
    | exn -> show_error ("The game server returned an invalid action result: " ^ Exn.to_string exn))
;;

let on_action_failure =
  Js.wrap_callback (fun message ->
    action_pending := false;
    Bonsai.Expert.Var.set busy_var false;
    show_error (Js.to_string message))
;;

let request_state () =
  ignore
    (Unsafe.meth_call
       api
       "state"
       [| Unsafe.inject on_state_success; Unsafe.inject on_state_failure |]
     : Unsafe.any)
;;

let dispatch_action ?text ?value state action_id =
  if not !action_pending
  then (
    action_pending := true;
    Bonsai.Expert.Var.set busy_var true;
    let value_argument =
      match value with
      | Some value -> Unsafe.inject value
      | None -> Unsafe.inject Js.null
    in
    ignore
      (Unsafe.meth_call
         api
         "action"
         [| Unsafe.inject (Js.string action_id)
          ; Unsafe.inject state.revision
          ; Unsafe.inject (Js.string (Option.value text ~default:""))
          ; value_argument
          ; Unsafe.inject on_action_success
          ; Unsafe.inject on_action_failure
         |]
       : Unsafe.any))
;;

let action_effect ?text ?value state action_id =
  Effect.of_sync_fun (fun () -> dispatch_action ?text ?value state action_id) ()
;;

let set_draft_effect screen_id value =
  Effect.of_sync_fun
    (fun () -> Bonsai.Expert.Var.set draft_var { screen_id; value })
    ()
;;

let class_ name = Vdom.Attr.class_ name
let attr name value = Vdom.Attr.create name value
let data name value = attr ("data-" ^ name) value
let money value = sprintf "$%.2f" value
let number value = Int.to_string value

let humanize value =
  let buffer = Buffer.create (String.length value + 8) in
  String.iteri value ~f:(fun index character ->
    if Char.equal character '_'
    then Buffer.add_char buffer ' '
    else (
      if index > 0
         && Char.is_uppercase character
         && Char.is_lowercase (String.get value (index - 1))
      then Buffer.add_char buffer ' ';
      Buffer.add_char buffer character));
  Buffer.contents buffer |> String.strip
;;

let screen_kind_label = function
  | "setup" -> "Journey setup"
  | "travel" -> "Travel dashboard"
  | "dialog" -> "On the trail"
  | "choice" -> "Decision"
  | "store" -> "Travel center"
  | "status" -> "Trip status"
  | "river" -> "River crossing"
  | "activity" -> "On the road"
  | "event" -> "Trail event"
  | "game-over" -> "Journey complete"
  | kind -> humanize kind
;;

let button
      ?(enabled = true)
      ?(primary = false)
      ?aria_label
      ?action_id
      ?action_kind
      ?(selected = false)
      ~label
      ~on_click
      ()
  =
  Vdom.Node.button
    ~attrs:
      [ class_ (if primary then "action action--primary" else "action")
      ; Vdom.Attr.type_ "button"
      ; Vdom.Attr.on_click (fun _ -> on_click)
      ; (match action_id with
         | Some action_id -> data "action-id" action_id
         | None -> Vdom.Attr.empty)
      ; (match action_kind with
         | Some action_kind -> data "action-kind" action_kind
         | None -> Vdom.Attr.empty)
      ; (if selected then data "action-selected" "true" else Vdom.Attr.empty)
      ; (match aria_label with
         | Some label -> attr "aria-label" label
         | None -> Vdom.Attr.empty)
      ; (if enabled then Vdom.Attr.empty else Vdom.Attr.disabled)
      ]
    [ Vdom.Node.text label ]
;;

let metric ?key ~label value =
  Vdom.Node.div
    ~attrs:
      [ class_ "metric"
      ; (match key with
         | Some key -> data "metric" key
         | None -> Vdom.Attr.empty)
      ]
    [ Vdom.Node.span ~attrs:[ class_ "metric__label" ] [ Vdom.Node.text label ]
    ; Vdom.Node.strong ~attrs:[ class_ "metric__value" ] [ Vdom.Node.text value ]
    ]
;;

let progress_view (progress : progress) =
  let percent =
    if progress.total_miles <= 0
    then 0
    else Int.min 100 (progress.miles_traveled * 100 / progress.total_miles)
  in
  Vdom.Node.section
    ~attrs:
      [ class_ "progress-card"
      ; data "region" "route-progress"
      ; data "progress-percent" (number percent)
      ; attr "aria-label" "Route progress"
      ]
    [ Vdom.Node.div
        ~attrs:
          [ class_ "progress-track"
          ; attr "role" "progressbar"
          ; attr "aria-valuemin" "0"
          ; attr "aria-valuemax" (number progress.total_miles)
          ; attr "aria-valuenow" (number progress.miles_traveled)
          ; attr
              "aria-valuetext"
              (sprintf
                 "%d of %d miles, %d percent complete"
                 progress.miles_traveled
                 progress.total_miles
                 percent)
          ]
        [ Vdom.Node.span
            ~attrs:
              [ class_ "progress-track__fill"
              ; attr "style" (sprintf "width: %d%%" percent)
              ; attr "aria-hidden" "true"
              ]
            []
        ; Vdom.Node.span
            ~attrs:[ class_ "progress-track__label" ]
            [ Vdom.Node.text (sprintf "%d%% of route complete" percent) ]
        ]
    ; Vdom.Node.div
        ~attrs:[ class_ "progress-copy" ]
        [ Vdom.Node.strong
            ~attrs:[ class_ "progress-copy__value" ]
            [ Vdom.Node.text (sprintf "%d miles traveled" progress.miles_traveled) ]
        ; Vdom.Node.span
            ~attrs:[ class_ "progress-copy__destination" ]
            [ Vdom.Node.text
                (if String.is_empty progress.next_location
                 then "Route planning"
                 else sprintf "%d miles to %s" progress.miles_to_next_location progress.next_location)
            ]
        ]
    ]
;;

let activity_progress (progress : progress) =
  match progress.activity_current, progress.activity_total with
  | Some current, Some total when total > 0 ->
    Vdom.Node.div
      ~attrs:
        [ class_ "activity-progress activity-progress--active"
        ; data "region" "activity-progress"
        ; data "activity-current" (number current)
        ; data "activity-total" (number total)
        ; attr "role" "progressbar"
        ; attr "aria-valuemin" "0"
        ; attr "aria-valuemax" (number total)
        ; attr "aria-valuenow" (number current)
        ; attr "aria-live" "polite"
        ]
      [ Vdom.Node.strong
          ~attrs:[ class_ "activity-progress__label" ]
          [ Vdom.Node.text
              (if String.is_empty progress.activity_label
               then "Activity in progress"
               else progress.activity_label)
          ]
      ; Vdom.Node.span
          ~attrs:[ class_ "activity-progress__value" ]
          [ Vdom.Node.text (sprintf "%d of %d" current total) ]
      ]
  | _ -> Vdom.Node.none
;;

let actions_view (state : game_state) busy (actions : game_action list) =
  let visible_actions =
    List.filter actions ~f:(fun (action : game_action) ->
      not (String.equal action.kind "adjust" || String.equal action.kind "set-value"))
  in
  Vdom.Node.div
    ~attrs:
      [ class_ "actions"
      ; data "region" "actions"
      ; attr "role" "group"
      ; attr "aria-label" "Available actions"
      ]
    (List.map visible_actions ~f:(fun (action : game_action) ->
       let primary =
         String.equal action.kind "primary"
         || String.equal action.kind "restart"
         || String.equal action.kind "continue"
       in
       button
         ~enabled:(action.enabled && not busy)
         ~primary
         ~action_id:action.action_id
         ~action_kind:action.kind
         ~selected:action.selected
         ~label:action.label
         ~on_click:(action_effect state action.action_id)
         ()))
;;

let input_view (state : game_state) busy (draft : draft) (input : input_spec) =
  let value = if String.equal draft.screen_id state.screen.id then draft.value else "" in
  let input_id = "screen-input" in
  let numeric = String.equal input.kind "number" in
  let numeric_value = if numeric then Int.of_string_opt value else None in
  let numeric_in_range =
    match numeric_value with
    | None -> not numeric
    | Some value ->
      Option.value_map input.min ~default:true ~f:(fun min -> value >= min)
      && Option.value_map input.max ~default:true ~f:(fun max -> value <= max)
  in
  let valid =
    (not input.required || not (String.is_empty (String.strip value)))
    && numeric_in_range
  in
  let submit =
    action_effect
      ~text:value
      ?value:numeric_value
      state
      input.action_id
  in
  let handle_enter event =
    match Js_of_ocaml.Dom_html.Keyboard_code.of_event event with
    | Enter when valid && not busy -> Effect.Many [ Effect.Prevent_default; submit ]
    | Enter -> Effect.Prevent_default
    | _ -> Effect.Ignore
  in
  Vdom.Node.div
    ~attrs:
      [ class_ "input-panel"
      ; data "region" "screen-input"
      ; data "input-kind" input.kind
      ; data "input-action-id" input.action_id
      ; data "input-required" (if input.required then "true" else "false")
      ]
    [ Vdom.Node.label
        ~attrs:[ class_ "input-panel__label"; attr "for" input_id ]
        [ Vdom.Node.text input.label ]
    ; Vdom.Node.input
        ~attrs:
          [ class_ "screen-input"
          ; attr "id" input_id
          ; data "input-kind" input.kind
          ; Vdom.Attr.type_ (if numeric then "number" else "text")
          ; Vdom.Attr.value_prop value
          ; Vdom.Attr.placeholder input.placeholder
          ; attr "autocomplete" "off"
          ; (if busy then Vdom.Attr.disabled else Vdom.Attr.empty)
          ; (if input.required then attr "required" "" else Vdom.Attr.empty)
          ; (match input.min with
             | Some value -> attr "min" (number value)
             | None -> Vdom.Attr.empty)
          ; (match input.max with
             | Some value -> attr "max" (number value)
             | None -> Vdom.Attr.empty)
          ; (match input.max_length with
             | Some value -> attr "maxlength" (number value)
             | None -> Vdom.Attr.empty)
          ; Vdom.Attr.on_input (fun _ value -> set_draft_effect state.screen.id value)
          ; Vdom.Attr.on_keydown handle_enter
          ]
        ()
    ; button
        ~enabled:
          (not busy && valid)
        ~primary:true
        ~action_id:input.action_id
        ~action_kind:"input"
        ~label:"Submit"
        ~on_click:submit
        ()
    ]
;;

let party_view (party : party_member list) =
  Vdom.Node.section
    ~attrs:
      [ class_ "side-card side-card--party"
      ; data "region" "party"
      ; data "empty" (if List.is_empty party then "true" else "false")
      ; attr "aria-labelledby" "party-heading"
      ]
    [ Vdom.Node.h2 ~attrs:[ attr "id" "party-heading" ] [ Vdom.Node.text "Party" ]
    ; (if List.is_empty party
       then Vdom.Node.p ~attrs:[ class_ "muted" ] [ Vdom.Node.text "Party not assembled yet." ]
       else
         Vdom.Node.ul
           ~attrs:[ class_ "party-list" ]
           (List.map party ~f:(fun (person : party_member) ->
             Vdom.Node.li
                ~attrs:
                  [ class_
                      (if person.is_leader
                       then "party-list__item party-list__item--leader"
                       else "party-list__item")
                  ; data "member-name" person.name
                  ; data "leader" (if person.is_leader then "true" else "false")
                  ; data "health" person.health
                  ]
                [ Vdom.Node.strong
                    [ Vdom.Node.text
                        (person.name ^ if person.is_leader then " · Driver" else "")
                    ]
                ; Vdom.Node.span
                    [ Vdom.Node.text
                        (humanize person.profession ^ " · " ^ humanize person.health)
                    ]
                ])))
    ]
;;

let inventory_view (inventory : inventory_item list) =
  let stocked = List.filter inventory ~f:(fun (item : inventory_item) -> item.quantity > 0) in
  Vdom.Node.section
    ~attrs:
      [ class_ "side-card side-card--inventory"
      ; data "region" "inventory"
      ; data "empty" (if List.is_empty stocked then "true" else "false")
      ; attr "aria-labelledby" "inventory-heading"
      ]
    [ Vdom.Node.h2 ~attrs:[ attr "id" "inventory-heading" ] [ Vdom.Node.text "Supplies" ]
    ; (if List.is_empty stocked
       then Vdom.Node.p ~attrs:[ class_ "muted" ] [ Vdom.Node.text "No supplies packed yet." ]
       else
         Vdom.Node.ul
           ~attrs:[ class_ "inventory-list" ]
           (List.map stocked ~f:(fun (item : inventory_item) ->
             Vdom.Node.li
                ~attrs:
                  [ class_ "inventory-list__item"
                  ; data "item-id" item.item_id
                  ; data "quantity" (number item.quantity)
                  ; data "unit" item.unit
                  ]
                [ Vdom.Node.span [ Vdom.Node.text item.name ]
                ; Vdom.Node.strong
                    [ Vdom.Node.text
                        (if String.is_empty item.unit
                         then number item.quantity
                         else sprintf "%d %s" item.quantity (humanize item.unit))
                    ]
                ])))
    ]
;;

let store_view (state : game_state) busy (store : store) =
  let checkout_actions =
    List.filter state.screen.actions ~f:(fun (action : game_action) ->
      not (String.equal action.kind "adjust" || String.equal action.kind "set-value"))
  in
  Vdom.Node.div
    ~attrs:
      [ class_ "store-view screen-body screen-body--store"
      ; data "region" "store"
      ; data "location" store.location
      ]
    [ Vdom.Node.section
        ~attrs:
          [ class_ "store-summary"
          ; data "region" "purchase-summary"
          ; attr "aria-label" "Purchase summary"
          ]
        [ metric ~key:"cash" ~label:"Cash" (money store.balance)
        ; metric ~key:"pending" ~label:"Pending" (money store.pending_total)
        ; metric
            ~key:"after-purchase"
            ~label:"After purchase"
            (money (Float.max 0. (store.balance -. store.pending_total)))
        ; metric
            ~key:"cargo"
            ~label:"Cargo"
            (sprintf "%d / %d lb" store.cargo_weight store.cargo_capacity)
        ]
    ; Vdom.Node.section
        ~attrs:
          [ class_ "store-list"
          ; data "region" "store-inventory"
          ; attr "aria-label" "Travel center inventory"
          ]
        (List.map store.rows ~f:(fun (row : store_row) ->
           Vdom.Node.create "article"
             ~attrs:
               [ class_
                   (if row.selected then "store-row store-row--selected" else "store-row")
               ; data "item-id" row.item_id
               ; data "selected" (if row.selected then "true" else "false")
               ; data "quantity" (number row.quantity)
               ; data "min-quantity" (number row.min_quantity)
               ; data "max-quantity" (number row.max_quantity)
               ]
             [ Vdom.Node.div
                 ~attrs:[ class_ "store-row__copy" ]
                 [ Vdom.Node.h3 [ Vdom.Node.text row.name ]
                 ; Vdom.Node.span
                     [ Vdom.Node.text
                         (sprintf "%s each · %s pending" (money row.unit_price) (money row.total_price))
                     ]
                 ]
             ; Vdom.Node.div
                 ~attrs:
                   [ class_ "stepper"
                   ; data "region" "quantity-stepper"
                   ; data "item-id" row.item_id
                   ; attr "aria-label" (row.name ^ " quantity")
                   ]
                 [ button
                     ~enabled:(not busy && row.quantity > row.min_quantity)
                     ~aria_label:("Decrease " ^ row.name)
                     ~action_id:row.decrease_action_id
                     ~action_kind:"adjust"
                     ~label:"−"
                     ~on_click:(action_effect state row.decrease_action_id)
                     ()
                 ; Vdom.Node.create "output"
                     ~attrs:
                       [ class_ "stepper__value"
                       ; data "quantity" (number row.quantity)
                       ; attr "aria-live" "polite"
                       ; attr "aria-label" (row.name ^ " quantity selected")
                       ]
                     [ Vdom.Node.text (number row.quantity) ]
                 ; button
                     ~enabled:(not busy && row.quantity < row.max_quantity)
                     ~aria_label:("Increase " ^ row.name)
                     ~action_id:row.increase_action_id
                     ~action_kind:"adjust"
                     ~label:"+"
                     ~on_click:(action_effect state row.increase_action_id)
                     ()
                 ]
             ; Vdom.Node.span
                 ~attrs:[ class_ "store-row__limit" ]
                 [ Vdom.Node.text (sprintf "Maximum now: %d" row.max_quantity) ]
             ]))
    ; actions_view state busy checkout_actions
    ]
;;

let purpose_view (state : game_state) busy (draft : draft) =
  let non_input_actions =
    match state.screen.input with
    | None -> state.screen.actions
    | Some input ->
      List.filter state.screen.actions ~f:(fun (action : game_action) ->
        not (String.equal action.action_id input.action_id))
  in
  let common_actions () =
    Vdom.Node.div
      ~attrs:[ class_ "screen-controls"; data "region" "screen-controls" ]
      [ (match state.screen.input with
         | Some input -> input_view state busy draft input
         | None -> Vdom.Node.none)
      ; actions_view state busy non_input_actions
      ]
  in
  match state.screen.kind, state.store with
  | "store", Some store -> store_view state busy store
  | "travel", _ ->
    Vdom.Node.div
      ~attrs:[ class_ "screen-body screen-body--travel"; data "region" "travel" ]
      [ progress_view state.progress
      ; Vdom.Node.div
          ~attrs:[ class_ "travel-facts"; data "region" "travel-facts" ]
          [ metric ~key:"weather" ~label:"Weather" (humanize state.hud.weather)
          ; metric ~key:"health" ~label:"Health" (humanize state.hud.health)
          ; metric ~key:"pace" ~label:"Pace" (humanize state.hud.pace)
          ; metric ~key:"rations" ~label:"Rations" (humanize state.hud.rations)
          ]
      ; common_actions ()
      ]
  | "activity", _ ->
    Vdom.Node.div
      ~attrs:[ class_ "screen-body screen-body--activity"; data "region" "activity" ]
      [ activity_progress state.progress; progress_view state.progress; common_actions () ]
  | "river", _ ->
    Vdom.Node.div
      ~attrs:
        [ class_ "screen-body screen-body--river mode-callout mode-callout--river"
        ; data "region" "river-crossing"
        ; data "mode" "river"
        ]
      [ Vdom.Node.p
          ~attrs:[ class_ "mode-callout__hint" ]
          [ Vdom.Node.text "Crossing conditions can change quickly." ]
      ; activity_progress state.progress
      ; common_actions ()
      ]
  | "event", _ ->
    Vdom.Node.div
      ~attrs:
        [ class_ "screen-body screen-body--event mode-callout mode-callout--event"
        ; data "region" "trail-event"
        ; data "mode" "event"
        ; attr "aria-live" "polite"
        ]
      [ Vdom.Node.p
          ~attrs:[ class_ "mode-callout__hint" ]
          [ Vdom.Node.text "This event may change party health, supplies, or time." ]
      ; common_actions ()
      ]
  | "status", _ ->
    Vdom.Node.div
      ~attrs:[ class_ "screen-body screen-body--status"; data "region" "status" ]
      [ progress_view state.progress; inventory_view state.inventory; common_actions () ]
  | "game-over", _ ->
    Vdom.Node.div
      ~attrs:
        [ class_ "screen-body screen-body--game-over mode-callout mode-callout--end"
        ; data "region" "journey-complete"
        ; data "mode" "game-over"
        ]
      [ metric ~key:"miles-traveled" ~label:"Miles traveled" (number state.progress.miles_traveled)
      ; metric ~key:"turns" ~label:"Turns" (number state.hud.turns)
      ; common_actions ()
      ]
  | "setup", _ | "dialog", _ | "choice", _ | _, _ ->
    Vdom.Node.div
      ~attrs:
        [ class_ ("screen-body screen-body--" ^ state.screen.kind)
        ; data "region" "screen"
        ]
      [ common_actions () ]
;;

let ready_view (state : game_state) busy reconnecting (draft : draft) =
  Vdom.Node.div
    ~attrs:
      [ class_ ("app app--" ^ state.screen.kind)
      ; data "screen-kind" state.screen.kind
      ; data "screen-id" state.screen.id
      ; data "revision" (number state.revision)
      ; data "running" (if state.running then "true" else "false")
      ; data "busy" (if busy then "true" else "false")
      ]
    [ Vdom.Node.header
        ~attrs:[ class_ "masthead"; data "region" "masthead" ]
        [ Vdom.Node.div
            ~attrs:[ class_ "brand-block" ]
            [ Vdom.Node.p ~attrs:[ class_ "eyebrow" ] [ Vdom.Node.text "The 2028 run" ]
            ; Vdom.Node.p ~attrs:[ class_ "brand" ] [ Vdom.Node.text "The Asphalt Trail" ]
            ]
        ; Vdom.Node.div
            ~attrs:
              [ class_ ("connection" ^ if reconnecting then " connection--reconnecting" else "")
              ; data "state" (if reconnecting then "reconnecting" else if busy then "updating" else "live")
              ; attr "role" "status"
              ; attr "aria-live" "polite"
              ]
            [ Vdom.Node.span ~attrs:[ class_ "connection__dot"; attr "aria-hidden" "true" ] []
            ; Vdom.Node.text
                (if reconnecting then "Reconnecting" else if busy then "Updating" else "Live")
            ]
        ]
    ; Vdom.Node.main
        ~attrs:
          [ class_ "shell"
          ; data "region" "main"
          ; attr "id" "main-content"
          ; attr "aria-busy" (if busy then "true" else "false")
          ]
        [ Vdom.Node.section
            ~attrs:
              [ class_ "hud"
              ; data "region" "hud"
              ; attr "aria-label" "Trip status"
              ]
            [ metric ~key:"date" ~label:"Date" state.hud.date
            ; metric
                ~key:"location"
                ~label:"Location"
                (state.hud.location_name ^ " · " ^ humanize state.hud.location_status)
            ; metric
                ~key:"vehicle"
                ~label:"Ride"
                (state.hud.vehicle_name ^ " · " ^ humanize state.hud.vehicle_status)
            ; metric ~key:"cash" ~label:"Cash" (money state.hud.balance)
            ]
        ; Vdom.Node.div
            ~attrs:[ class_ "page-grid"; data "region" "journey-layout" ]
            [ Vdom.Node.create "article"
                ~attrs:
                  [ class_ ("screen-card screen-card--" ^ state.screen.kind)
                  ; data "region" "screen"
                  ; data "screen-kind" state.screen.kind
                  ; data "screen-id" state.screen.id
                  ; attr "aria-labelledby" "screen-title"
                  ; attr "aria-describedby" "screen-description"
                  ]
                [ Vdom.Node.header
                    ~attrs:[ class_ "screen-card__header"; data "region" "screen-heading" ]
                    [ Vdom.Node.p
                        ~attrs:[ class_ "screen-kind"; data "screen-kind-label" state.screen.kind ]
                        [ Vdom.Node.text (screen_kind_label state.screen.kind) ]
                    ; Vdom.Node.h1
                        ~attrs:
                          [ attr "id" "screen-title"
                          ; attr "tabindex" "-1"
                          ; attr "data-screen-focus" ""
                          ]
                        [ Vdom.Node.text (humanize state.screen.title) ]
                    ; Vdom.Node.p
                        ~attrs:[ class_ "lede"; attr "id" "screen-description" ]
                        [ Vdom.Node.text state.screen.description ]
                    ]
                ; (if String.is_empty state.response_message
                   then Vdom.Node.none
                   else
                     Vdom.Node.p
                       ~attrs:
                         [ class_ "response-message"
                         ; data "region" "response"
                         ; attr "role" "alert"
                         ]
                       [ Vdom.Node.text state.response_message ])
                ; purpose_view state busy draft
                ]
            ; Vdom.Node.create "aside"
                ~attrs:
                  [ class_ "sidebar"
                  ; data "region" "journey-details"
                  ; attr "aria-label" "Journey details"
                  ]
                [ party_view state.party; inventory_view state.inventory ]
            ]
        ]
    ; Vdom.Node.footer
        ~attrs:[ class_ "footer"; data "region" "footer" ]
        [ Vdom.Node.text "The Asphalt Trail · Cape Coral to Seattle" ]
    ]
;;

let component graph =
  let connection = Bonsai.Expert.Var.value connection_var in
  let busy = Bonsai.Expert.Var.value busy_var in
  let reconnecting = Bonsai.Expert.Var.value reconnecting_var in
  let draft = Bonsai.Expert.Var.value draft_var in
  let%arr connection = connection
  and busy = busy
  and reconnecting = reconnecting
  and draft = draft in
  match connection with
  | Loading ->
    Vdom.Node.main
      ~attrs:
        [ class_ "state-page state-page--loading"
        ; data "state" "loading"
        ; attr "id" "main-content"
        ; attr "aria-busy" "true"
        ]
      [ Vdom.Node.div
          ~attrs:
            [ class_ "state-card state-card--loading"
            ; data "region" "loading"
            ; attr "role" "status"
            ]
          [ Vdom.Node.span ~attrs:[ class_ "loading-mark"; attr "aria-hidden" "true" ] [ Vdom.Node.text "AT" ]
          ; Vdom.Node.h1 [ Vdom.Node.text "Loading the trail" ]
          ; Vdom.Node.p [ Vdom.Node.text "Preparing a semantic game session…" ]
          ]
      ]
  | Failed message ->
    Vdom.Node.main
      ~attrs:
        [ class_ "state-page state-page--error"
        ; data "state" "error"
        ; attr "id" "main-content"
        ]
      [ Vdom.Node.section
          ~attrs:
            [ class_ "state-card state-card--error"
            ; data "region" "error"
            ; attr "role" "alert"
            ]
          [ Vdom.Node.p ~attrs:[ class_ "eyebrow" ] [ Vdom.Node.text "Connection lost" ]
          ; Vdom.Node.h1 [ Vdom.Node.text "The trail is out of reach" ]
          ; Vdom.Node.p [ Vdom.Node.text message ]
          ; button
              ~primary:true
              ~label:"Try again"
              ~on_click:(Effect.of_sync_fun request_state ())
              ()
          ]
      ]
  | Ready state -> ready_view state busy reconnecting draft
;;

let () =
  Bonsai_web.Start.start ~bind_to_element_with_id:"app" component;
  request_state ();
  ignore
    (Unsafe.meth_call
       Unsafe.global
       "setInterval"
       [| Unsafe.inject (Js.wrap_callback request_state); Unsafe.inject 500 |]
     : Unsafe.any)
;;
