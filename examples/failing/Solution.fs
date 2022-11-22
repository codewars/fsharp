module OddOrEvenKata

open Preloaded

let oddOrEven n = match n % 2 with
                  | 1 -> Answer.Even
                  | _ -> Answer.Odd

