﻿namespace DataGridWithButtonReset

open System
open Elmish
open Elmish.WPF
open DataGridWithButtonReset.View


module Cell =

    type Model =
        { Id: Guid
          CellName: string }

    let init i j k=
        { Id = Guid.NewGuid ()
          CellName = sprintf "CellName %i  %i  %i" i j k }

    let bindings() = [
        "LastName" |> Binding.oneWay(fun (_, c) -> c.CellName)
        "SelectedLabel" |> Binding.oneWay (fun (b, _) -> if b then " - Selected" else "")
     ]


module Column =

    type Model =
        { Id: Guid
          InnerRows: Cell.Model list
          SelectedInnerRow: Guid option }

    type Msg =
        | Select of Guid option

    let init i j =
        { Id = Guid.NewGuid ()
          InnerRows = [0 .. 2] |> List.map (Cell.init i j) 
          SelectedInnerRow = None }

    let isSelectMsg = function
      | Select (Some _) -> true
      | _ -> false

    let deselect m =
      { m with SelectedInnerRow = None }
          
    let update msg m =
        match msg with
        | Select id -> { m with SelectedInnerRow = id }

    let bindings () = [
        "InnerRows" |> Binding.subModelSeq(
            (fun (_, p) -> p.InnerRows),
            (fun ((_, p), c) -> (p.SelectedInnerRow = Some c.Id, c)),
            (fun (_, c) -> c.Id),
            snd,
            Cell.bindings)

        "SelectedInnerRow" |> Binding.subModelSelectedItem("InnerRows", (fun (_, r) -> r.SelectedInnerRow), (fun cId _ -> Select cId))
    ]


module OutterRow =

    type Model =
      { Id: Guid
        OutterRowName: string
        Columns: Column.Model list 
        SelectedColumn: Guid option }

    type Msg =
        | Select of Guid option
        | ColumnMsg of Guid * Column.Msg
     

    let init i =
        { Id = Guid.NewGuid ()
          OutterRowName = sprintf "RowName %i" i
          Columns =  [0 .. 3] |> List.map (Column.init i) 
          SelectedColumn = None }

    let isSelectCellMsg = function
      | ColumnMsg (_, msg) -> msg |> Column.isSelectMsg
      | _ -> false

    let deselectCells m =
      { m with Columns = m.Columns |> List.map Column.deselect }

    let deselectAll m =
      { m with SelectedColumn = None } |> deselectCells
    
    let update msg m =
      match msg with
      | Select id -> { m with SelectedColumn = id }
      | ColumnMsg (rId, msg) ->
          let columns =
            m.Columns
            |> List.map (fun r -> if r.Id = rId then Column.update msg r else r)
          { m with Columns = columns }

   (*
    let reset msg m =
        match msg with
        | ResetMsg _ -> 
            let columns =
              m.Columns
              |> List.map ( fun r -> Column.reset 1  1)
            { m with Columns = columns } 
   *)

    let bindings () = [
        "RowTime" |> Binding.oneWay(fun (b, p) -> p.OutterRowName + (if b then " - Selected" else ""))

        "Columns" |> Binding.subModelSeq(
                        (fun (_, p) -> p.Columns),
                        (fun ((b, p), c) -> (b && p.SelectedColumn = Some c.Id, c)),  // b is true when this Column is in the selected outer row.
                        (fun (_, c) -> c.Id),
                        ColumnMsg,
                        Column.bindings)
    ]

    
module App =

   type Model =
      { OutterRows: OutterRow.Model list
        SelectedOutterRow: Guid option }

   type Msg =
      | Select of Guid option
      | RowMsg of Guid * OutterRow.Msg
      | Reset
      | DeselectAll
      | DeselectCells


   let init () =
     {  OutterRows = [0 .. 2] |> List.map OutterRow.init
        SelectedOutterRow = None }

   let deselectCells m =
     { m with OutterRows = m.OutterRows |> List.map OutterRow.deselectCells }

   let deselectAll m =
     { m with SelectedOutterRow = None
              OutterRows = m.OutterRows |> List.map OutterRow.deselectAll }

   let updateOutterRow rId msg m =
     let rows =
       m.OutterRows
       |> List.map (fun r -> if r.Id = rId then OutterRow.update msg r else r )  // OutterRow.reset msg r
     { m with OutterRows = rows }

   let update msg m =
      match msg with
      | Select rId -> { m with SelectedOutterRow = rId }
      | RowMsg (rId, msg) ->
          m
          |> if OutterRow.isSelectCellMsg msg then deselectCells else id
          |> updateOutterRow rId msg
      | Reset -> init ()
      | DeselectAll -> m |> deselectAll
      | DeselectCells -> m |> deselectCells


   let bindings () : Binding<Model,Msg> list  = [
        "Rows" |> Binding.subModelSeq(
            (fun m -> m.OutterRows),
            (fun (m, r) -> (m.SelectedOutterRow = Some r.Id, r)),
            (fun (_, r) -> r.Id),
            RowMsg,
            OutterRow.bindings)

        "SelectedRow" |> Binding.subModelSelectedItem("Rows", (fun m -> m.SelectedOutterRow), Select)

        "Reset" |> Binding.cmd (fun m -> Reset)  // Reset is a Msg. The Msg is acted upon by update.
        "DeselectAll" |> Binding.cmd (fun m -> DeselectAll)
        "DeselectCells" |> Binding.cmd (fun m -> DeselectCells)
   ]

    [<EntryPoint; STAThread>]
    let main argv =
      Program.mkSimpleWpf init update bindings
      |> Program.runWindowWithConfig
        { ElmConfig.Default with LogConsole = true }
        (MainWindow())
 