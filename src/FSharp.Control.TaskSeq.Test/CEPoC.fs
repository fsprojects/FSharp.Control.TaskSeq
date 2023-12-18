namespace TaskSeq.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Control


module CEs =

    type M<'T, 'Vars> = {
        Name: string option
        IsMember: bool option
        IsMember2: bool // confirming that shape of the container is not restricted, just need clear default the CE understands
        Members: 'T list
        Variables: 'Vars
    }

    type M<'T> = M<'T, unit>

    type CE() =

        member _.Zero() : M<'T> = {
            Name = None
            IsMember = None
            IsMember2 = false
            Members = []
            Variables = ()
        }

        member _.Combine(model1: M<'T>, model2: M<'T>) : M<'T> =
            let newName =
                match model2.Name with
                | None -> model1.Name
                | res -> res

            let newIsMember =
                match model2.IsMember with
                | None -> model1.IsMember
                | res -> res

            let newIsMember2 =
                match model2.IsMember2 with
                | true -> true
                | res -> res

            {
                Name = newName
                IsMember = newIsMember
                IsMember2 = newIsMember2
                Members = List.append model1.Members model2.Members
                Variables = ()
            }

        member _.Delay(f) : M<'T, 'Vars> = f ()

        member _.Run(model: M<'T, 'Vars>) : M<'T> = {
            Name = model.Name
            IsMember = model.IsMember
            IsMember2 = model.IsMember2
            Members = model.Members
            Variables = ()
        }

        member this.For(methods, f) : M<'T> =
            let methodList = Seq.toList methods

            match methodList with
            | [] -> this.Zero()
            | [ x ] -> f (x)
            | head :: tail ->
                let mutable headResult = f (head)

                for x in tail do
                    headResult <- this.Combine(headResult, f (x))

                headResult

        member _.Yield(item: 'T) : M<'T> = {
            Name = None
            IsMember = None
            IsMember2 = false
            Members = [ item ]
            Variables = ()
        }

        // Only for packing/unpacking the implicit variable space
        member _.Bind(model1: M<'T, 'Vars>, f: ('Vars -> M<'T>)) : M<'T> =
            let model2 = f model1.Variables

            let newName =
                match model2.Name with
                | None -> model1.Name
                | res -> res

            let newIsMember =
                match model2.IsMember with
                | None -> model1.IsMember
                | res -> res

            let newIsMember2 =
                match model2.IsMember2 with
                | true -> true
                | res -> res

            {
                Name = newName
                IsMember = newIsMember
                IsMember2 = newIsMember2
                Members = model1.Members @ model2.Members
                Variables = model2.Variables
            }

        // Only for packing/unpacking the implicit variable space
        member _.Return(varspace: 'Vars) : M<'T, 'Vars> = {
            Name = None
            IsMember = None
            IsMember2 = false
            Members = []
            Variables = varspace
        }

        [<CustomOperation("Name", MaintainsVariableSpaceUsingBind = true)>]
        member _.setName(model: M<'T, 'Vars>, [<ProjectionParameter>] name: ('Vars -> string)) : M<'T, 'Vars> = {
            model with
                Name = Some(name model.Variables)
        }

        [<CustomOperation("IsMember", MaintainsVariableSpaceUsingBind = true)>]
        member _.setIsMember(model: M<'T, 'Vars>, [<ProjectionParameter>] isMember: ('Vars -> bool)) : M<'T, 'Vars> = {
            model with
                IsMember = Some(isMember model.Variables)
        }

        // We can skip
        [<CustomOperation("IsMember2", MaintainsVariableSpaceUsingBind = true)>]
        member _.setIsMember2(model: M<'T, 'Vars>, [<ProjectionParameter>] isMember: ('Vars -> bool)) : M<'T, 'Vars> = {
            model with
                IsMember2 = isMember model.Variables
        }

        [<CustomOperation("Member", MaintainsVariableSpaceUsingBind = true)>]
        member _.addMember(model: M<'T, 'Vars>, [<ProjectionParameter>] item: ('Vars -> 'T)) : M<'T, 'Vars> = {
            model with
                Members = List.append model.Members [ item model.Variables ]
        }

        // Note, using ParamArray doesn't work in conjunction with ProjectionParameter
        [<CustomOperation("Members", MaintainsVariableSpaceUsingBind = true)>]
        member _.addMembers(model: M<'T, 'Vars>, [<ProjectionParameter>] items: ('Vars -> 'T list)) : M<'T, 'Vars> = {
            model with
                Members = List.append model.Members (items model.Variables)
        }

    let ce = CE()

    module Test =
        let x: M<double> = ce { Name "Fred" }

        let x2: M<double> = ce {
            Name "Fred"
            IsMember true
            IsMember true
            IsMember2 true // Note, I can call this twice without compiler error, but not Name in z5
            IsMember2 true
        }

        let y = ce { 42 }

        let z1 = ce { Member 42 }

        let z2 = ce { Members [ 41; 42 ] }

        let z3 = ce {
            Name "Fred"
            42
        }

        let z4 = ce {
            Member 41
            Member 42
        }

        let z5: M<double> = ce {
            Name "a"
            Name "b"
        //42 // removing this line results in compiler error
        }

        let z6: M<double> = ce {
            let x = 1
            let y = 2
            let z = 3
            let! foo = Unchecked.defaultof<M<double>>
            Name "a"
            Member 4.0
        }

        let z7: M<double> = ce {
            let x = "a"
            Name(x + "b")
            Member 4.0
        }

        let z8: M<double> = ce {
            let x1 = 1.0
            let y2 = 2.0
            Member(x1 + 3.0)
            Member(y2 + 4.0)
        }

        let z9 = ce {
            let x = 1.0
            Members [ 42.3; 43.1 + x ]
        }
//let empty =
//    ce { }
